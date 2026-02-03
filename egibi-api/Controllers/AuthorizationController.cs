#nullable disable
using System.Security.Claims;
using egibi_api.Data;
using egibi_api.Services;
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
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictScopeManager _scopeManager;

        public AuthorizationController(
            EgibiDbContext db,
            IEncryptionService encryption,
            AppUserService userService,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictScopeManager scopeManager)
        {
            _db = db;
            _encryption = encryption;
            _userService = userService;
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

            // Create a cookie-based identity for the authorize endpoint
            var identity = new ClaimsIdentity("EgibiCookie");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()));
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));

            await HttpContext.SignInAsync("EgibiCookie", new ClaimsPrincipal(identity));

            return Ok(new
            {
                message = "Authenticated",
                email = user.Email,
                role = user.Role,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }

        // =============================================
        // POST /auth/signup
        // =============================================
        [HttpPost("~/auth/signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                return BadRequest(new { error = "Email and password are required." });

            var (user, error) = await _userService.CreateUserAsync(
                request.Email, request.Password, request.FirstName, request.LastName);

            if (user == null)
                return BadRequest(new { error });

            // Auto-login after signup: set the cookie so the SPA can immediately
            // proceed with the OIDC authorize flow
            var identity = new ClaimsIdentity("EgibiCookie");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()));
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));

            await HttpContext.SignInAsync("EgibiCookie", new ClaimsPrincipal(identity));

            return Ok(new
            {
                message = "Account created",
                email = user.Email,
                role = user.Role,
                firstName = user.FirstName,
                lastName = user.LastName
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
            // In production, the token would be sent via email.
            if (token != null)
            {
                // TODO: Send email with reset link:
                //   {spaBaseUrl}/auth/reset-password?email={email}&token={token}
                // For now, log it in development for testing
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthorizationController>>();
                logger.LogWarning(
                    "DEV ONLY — Password reset token for {Email}: {Token}",
                    request.Email, token);
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
}
