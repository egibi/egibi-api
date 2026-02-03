#nullable disable
using System.Security.Claims;
using egibi_api.Data;
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
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictScopeManager _scopeManager;

        public AuthorizationController(
            EgibiDbContext db,
            IEncryptionService encryption,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictScopeManager scopeManager)
        {
            _db = db;
            _encryption = encryption;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _scopeManager = scopeManager;
        }

        // =============================================
        // POST /connect/token
        // =============================================
        // Handles:
        //   - authorization_code: exchanges auth code for tokens
        //   - refresh_token: refreshes an expired access token
        //   - client_credentials: service-to-service auth
        // =============================================
        [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                // Retrieve the claims principal stored in the authorization code / refresh token
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
        // For SPAs: the user is redirected here, authenticates, and gets
        // an authorization code back to the redirect URI.
        //
        // Since we don't have a login UI served by the API (the Angular
        // SPA handles login), we use a simplified approach:
        //   1. SPA collects credentials and POSTs to /auth/login
        //   2. On success, SPA initiates the OIDC auth code flow
        //   3. This endpoint auto-approves for authenticated users
        // =============================================
        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // If the user is not authenticated, challenge them
            var result = await HttpContext.AuthenticateAsync();
            if (!result.Succeeded || request.HasPrompt(Prompts.Login))
            {
                // For an API-based flow, return 401 so the SPA knows to show the login form
                // and then POST credentials to /auth/login first
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

                // Redirect to the SPA's login page, passing the return URL
                var redirectUri = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());

                return Challenge(
                    authenticationSchemes: new[] { "EgibiCookie" },
                    properties: new AuthenticationProperties { RedirectUri = redirectUri });
            }

            // Auto-approve: create claims principal from the authenticated user
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
        public IActionResult Logout()
        {
            return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // =============================================
        // POST /auth/login  (Resource Owner — for SPA login form)
        // =============================================
        // This is NOT an OIDC endpoint. It's a convenience endpoint
        // for the SPA to authenticate the user and set a cookie,
        // so the OIDC authorize flow can proceed automatically.
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
        // Helpers
        // =============================================

        /// <summary>
        /// Determines which claims go into which tokens (access token, ID token, etc.)
        /// </summary>
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

                // Subject is always included in both tokens
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

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
