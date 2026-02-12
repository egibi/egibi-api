#nullable disable
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using egibi_api.Data;
using egibi_api.Services;
using egibi_api.Services.Email;
using egibi_api.Services.Security;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace egibi_api.Controllers
{
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly AppUserService _userService;
        private readonly MfaService _mfaService;
        private readonly AccessRequestService _accessRequestService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictScopeManager _scopeManager;

        public AuthorizationController(
            EgibiDbContext db,
            IEncryptionService encryption,
            AppUserService userService,
            MfaService mfaService,
            AccessRequestService accessRequestService,
            IEmailService emailService,
            IConfiguration config,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictScopeManager scopeManager)
        {
            _db = db;
            _encryption = encryption;
            _userService = userService;
            _mfaService = mfaService;
            _accessRequestService = accessRequestService;
            _emailService = emailService;
            _config = config;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _scopeManager = scopeManager;
        }

        // =============================================
        // POST /connect/token
        // =============================================
        [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                if (!result.Succeeded)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                        }));
                }

                var identity = (ClaimsIdentity)result.Principal.Identity;

                // Refresh claims from DB in case user info changed
                var userId = identity.FindFirst(Claims.Subject)?.Value;
                if (userId != null)
                {
                    var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(userId) && u.IsActive);
                    if (user == null)
                    {
                        return Forbid(
                            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                            properties: new AuthenticationProperties(new Dictionary<string, string>
                            {
                                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user account has been deactivated."
                            }));
                    }
                }

                return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (request.IsClientCredentialsGrantType())
            {
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId);
                if (application == null)
                    throw new InvalidOperationException("The application details cannot be found.");

                var identity = new ClaimsIdentity(
                    authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
                identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));

                identity.SetScopes(request.GetScopes());
                identity.SetResources("egibi-api");

                var principal = new ClaimsPrincipal(identity);
                principal.SetDestinations(GetDestinations);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not supported.");
        }

        // =============================================
        // GET/POST /connect/authorize
        // =============================================
        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var result = await HttpContext.AuthenticateAsync("EgibiCookie");
            if (!result.Succeeded || request.HasPrompt(Prompts.Login))
            {
                if (request.HasPrompt(Prompts.None))
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                        }));
                }

                // Return the full authorize URL so the SPA can retry after login
                var redirectUri = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());

                return Challenge(
                    authenticationSchemes: new[] { "EgibiCookie" },
                    properties: new AuthenticationProperties { RedirectUri = redirectUri });
            }

            // Auto-approve: create claims principal from the authenticated cookie
            var userId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? result.Principal.FindFirst(Claims.Subject)?.Value;

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(userId) && u.IsActive);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user account was not found."
                    }));
            }

            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, user.Id.ToString());
            identity.SetClaim(Claims.Email, user.Email);
            identity.SetClaim(Claims.Name, $"{user.FirstName} {user.LastName}".Trim());
            identity.SetClaim(Claims.Role, user.Role);

            identity.SetScopes(request.GetScopes());
            identity.SetResources("egibi-api");

            var principal = new ClaimsPrincipal(identity);
            principal.SetDestinations(GetDestinations);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // =============================================
        // GET /connect/userinfo
        // =============================================
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {
            var userId = User.FindFirst(Claims.Subject)?.Value;
            if (userId == null)
                return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null)
                return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var claims = new Dictionary<string, object>
            {
                [Claims.Subject] = user.Id.ToString()
            };

            if (User.HasScope(Scopes.Email))
            {
                claims[Claims.Email] = user.Email;
            }

            if (User.HasScope(Scopes.Profile))
            {
                claims[Claims.Name] = $"{user.FirstName} {user.LastName}".Trim();
                claims[Claims.GivenName] = user.FirstName ?? "";
                claims[Claims.FamilyName] = user.LastName ?? "";
            }

            if (User.HasScope(Scopes.Roles))
            {
                claims[Claims.Role] = user.Role;
            }

            return Ok(claims);
        }

        // =============================================
        // POST /connect/logout
        // =============================================
        [HttpPost("~/connect/logout"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Also clear the login cookie
            await HttpContext.SignOutAsync("EgibiCookie");
            return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // =============================================
        // POST /auth/login  (Cookie-based — for SPA login form)
        // =============================================
        [HttpPost("~/auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                return BadRequest(new { error = "Email and password are required." });

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null || !_encryption.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { error = "Invalid email or password." });

            // --- MFA CHECK ---
            // If MFA is enabled, don't set the cookie yet.
            // Return a temporary MFA challenge token instead.
            if (user.IsMfaEnabled)
            {
                var mfaToken = GenerateMfaChallengeToken(user.Id);
                return Ok(new
                {
                    mfaRequired = true,
                    mfaToken
                });
            }

            // No MFA — proceed with cookie as before
            await SignInWithCookie(user);

            return Ok(new
            {
                mfaRequired = false,
                message = "Authenticated",
                email = user.Email,
                role = user.Role,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }

        // =============================================
        // POST /auth/mfa-verify  (Completes login when MFA is required)
        // =============================================
        [HttpPost("~/auth/mfa-verify")]
        [AllowAnonymous]
        public async Task<IActionResult> MfaVerify([FromBody] MfaVerifyLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.MfaToken))
                return BadRequest(new { error = "MFA token is required." });

            if (string.IsNullOrWhiteSpace(request?.Code) && string.IsNullOrWhiteSpace(request?.RecoveryCode))
                return BadRequest(new { error = "A verification code or recovery code is required." });

            // Validate the MFA challenge token
            var userId = ValidateMfaChallengeToken(request.MfaToken);
            if (userId == null)
                return Unauthorized(new { error = "MFA session has expired. Please log in again." });

            bool isValid;
            string error;

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                // Verify TOTP code
                (isValid, error) = await _mfaService.VerifyCodeAsync(userId.Value, request.Code);
            }
            else
            {
                // Verify recovery code
                (isValid, error) = await _mfaService.VerifyRecoveryCodeAsync(userId.Value, request.RecoveryCode);
            }

            if (!isValid)
                return Unauthorized(new { error = error ?? "Invalid verification code." });

            // MFA passed — set the cookie and complete login
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive);
            if (user == null)
                return Unauthorized(new { error = "User account not found." });

            await SignInWithCookie(user);

            return Ok(new
            {
                mfaRequired = false,
                message = "Authenticated",
                email = user.Email,
                role = user.Role,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }

        // =============================================
        // POST /auth/signup  (creates an AccessRequest with email verification)
        // =============================================
        [HttpPost("~/auth/signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                return BadRequest(new { error = "Email and password are required." });

            var (accessRequest, error) = await _accessRequestService.SubmitRequestAsync(
                request.Email, request.Password, request.FirstName, request.LastName);

            if (accessRequest == null)
                return BadRequest(new { error });

            return Ok(new
            {
                accessRequestSubmitted = true,
                emailVerificationRequired = true,
                message = "Please check your email to verify your address. Once verified, an administrator will review your request."
            });
        }

        // =============================================
        // POST /auth/verify-email
        // =============================================
        [HttpPost("~/auth/verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Token))
                return BadRequest(new { error = "Email and verification token are required." });

            var (success, error) = await _accessRequestService.VerifyEmailAsync(request.Email, request.Token);

            if (!success)
                return BadRequest(new { error });

            return Ok(new
            {
                verified = true,
                message = "Your email has been verified. An administrator will review your access request and you will be notified by email once a decision has been made."
            });
        }

        // =============================================
        // POST /auth/resend-verification
        // =============================================
        [HttpPost("~/auth/resend-verification")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
                return BadRequest(new { error = "Email is required." });

            var (success, error) = await _accessRequestService.ResendVerificationAsync(request.Email);

            if (!success)
                return BadRequest(new { error });

            return Ok(new
            {
                message = "A new verification email has been sent. Please check your inbox."
            });
        }

        // =============================================
        // POST /auth/forgot-password
        // =============================================
        [HttpPost("~/auth/forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
                return BadRequest(new { error = "Email is required." });

            var token = await _userService.GeneratePasswordResetTokenAsync(request.Email);

            // Always return success to prevent email enumeration attacks.
            if (token != null)
            {
                var frontendUrl = _config["App:FrontendUrl"] ?? "https://www.egibi.io";
                var resetLink = $"{frontendUrl}/auth/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendPasswordResetAsync(request.Email, resetLink);
                    }
                    catch (Exception ex)
                    {
                        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthorizationController>>();
                        logger.LogError(ex, "Failed to send password reset email for {Email}", request.Email);
                    }
                });
            }

            return Ok(new
            {
                message = "If an account with that email exists, a password reset link has been sent."
            });
        }

        // =============================================
        // POST /auth/reset-password
        // =============================================
        [HttpPost("~/auth/reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) ||
                string.IsNullOrWhiteSpace(request?.Token) ||
                string.IsNullOrWhiteSpace(request?.NewPassword))
            {
                return BadRequest(new { error = "Email, token, and new password are required." });
            }

            var (success, error) = await _userService.ResetPasswordAsync(
                request.Email, request.Token, request.NewPassword);

            if (!success)
                return BadRequest(new { error });

            return Ok(new { message = "Password has been reset. You can now log in." });
        }

        // =============================================
        // MFA CHALLENGE TOKEN HELPERS
        // =============================================

        /// <summary>
        /// Creates a short-lived, tamper-proof token that proves the user passed password verification.
        /// Contains: userId + expiry timestamp, signed via HMAC-SHA256 with the master key.
        /// </summary>
        private string GenerateMfaChallengeToken(int userId)
        {
            var expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
            var payload = $"{userId}:{expiry}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            // HMAC-SHA256 signature using the master key for tamper protection
            var masterKey = _encryption.GetMasterKeyBytes();
            using var hmac = new HMACSHA256(masterKey);
            var signature = hmac.ComputeHash(payloadBytes);

            // Combine payload + signature
            var combined = new byte[payloadBytes.Length + 1 + signature.Length];
            Buffer.BlockCopy(payloadBytes, 0, combined, 0, payloadBytes.Length);
            combined[payloadBytes.Length] = (byte)'.'; // separator
            Buffer.BlockCopy(signature, 0, combined, payloadBytes.Length + 1, signature.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Validates an MFA challenge token and returns the userId if valid and not expired.
        /// </summary>
        private int? ValidateMfaChallengeToken(string token)
        {
            try
            {
                var combined = Convert.FromBase64String(token);

                // Find the separator
                var sepIndex = Array.IndexOf(combined, (byte)'.');
                if (sepIndex < 0) return null;

                var payloadBytes = combined[..sepIndex];
                var receivedSignature = combined[(sepIndex + 1)..];

                // Verify HMAC signature
                var masterKey = _encryption.GetMasterKeyBytes();
                using var hmac = new HMACSHA256(masterKey);
                var expectedSignature = hmac.ComputeHash(payloadBytes);

                if (!CryptographicOperations.FixedTimeEquals(receivedSignature, expectedSignature))
                    return null;

                // Parse payload
                var payload = Encoding.UTF8.GetString(payloadBytes);
                var parts = payload.Split(':');
                if (parts.Length != 2) return null;

                var userId = int.Parse(parts[0]);
                var expiry = long.Parse(parts[1]);

                // Check expiry
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                    return null;

                return userId;
            }
            catch
            {
                return null;
            }
        }

        // =============================================
        // COOKIE HELPER
        // =============================================

        private async Task SignInWithCookie(Data.Entities.AppUser user)
        {
            var identity = new ClaimsIdentity("EgibiCookie");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()));
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));

            await HttpContext.SignInAsync("EgibiCookie", new ClaimsPrincipal(identity));
        }

        // =============================================
        // Helpers
        // =============================================

        private static IEnumerable<string> GetDestinations(Claim claim)
        {
            switch (claim.Type)
            {
                case Claims.Name or Claims.Email:
                    yield return Destinations.AccessToken;
                    if (claim.Subject?.HasScope(Scopes.Profile) == true ||
                        claim.Subject?.HasScope(Scopes.Email) == true)
                        yield return Destinations.IdentityToken;
                    yield break;

                case Claims.Role:
                    yield return Destinations.AccessToken;
                    if (claim.Subject?.HasScope(Scopes.Roles) == true)
                        yield return Destinations.IdentityToken;
                    yield break;

                case Claims.Subject:
                    yield return Destinations.AccessToken;
                    yield return Destinations.IdentityToken;
                    yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }
    }

    // =============================================
    // REQUEST MODELS
    // =============================================

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class SignupRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class MfaVerifyLoginRequest
    {
        public string MfaToken { get; set; }
        public string Code { get; set; }
        public string RecoveryCode { get; set; }
    }

    public class VerifyEmailRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }

    public class ResendVerificationRequest
    {
        public string Email { get; set; }
    }
}
