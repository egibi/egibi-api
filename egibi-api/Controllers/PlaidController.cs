#nullable disable
using System.Security.Claims;
using egibi_api.Data;
using egibi_api.Models;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PlaidController : ControllerBase
    {
        private readonly PlaidService _plaidService;
        private readonly EgibiDbContext _db;

        public PlaidController(PlaidService plaidService, EgibiDbContext db)
        {
            _plaidService = plaidService;
            _db = db;
        }

        /// <summary>
        /// Returns the current authenticated user's ID from the OIDC claims.
        /// </summary>
        private int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(Claims.Subject);
            return int.Parse(sub ?? throw new UnauthorizedAccessException("No subject claim found"));
        }

        /// <summary>
        /// Checks whether the user has MFA enabled. Returns a 403 RequestResponse if not.
        /// Plaid production access requires MFA before surfacing Plaid Link.
        /// </summary>
        private async Task<RequestResponse> RequireMfaAsync(int userId)
        {
            var user = await _db.AppUsers
                .AsNoTracking()
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => new { u.IsMfaEnabled })
                .FirstOrDefaultAsync();

            if (user == null)
                return new RequestResponse(null, 401, "User not found.");

            if (!user.IsMfaEnabled)
                return new RequestResponse(
                    new { mfaRequired = true },
                    403,
                    "Multi-factor authentication must be enabled before connecting a bank account. Please enable MFA in your Security settings.");

            return null; // MFA is enabled, proceed
        }

        // =============================================
        // PLAID CONFIG MANAGEMENT
        // =============================================

        /// <summary>
        /// Returns whether the user has Plaid credentials configured.
        /// </summary>
        [HttpGet("config/status")]
        public async Task<RequestResponse> GetConfigStatus()
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.GetPlaidConfigStatus(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Saves the user's Plaid developer credentials.
        /// </summary>
        [HttpPost("config")]
        public async Task<RequestResponse> SaveConfig([FromBody] PlaidConfigRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.SavePlaidConfig(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Deletes the user's Plaid configuration.
        /// </summary>
        [HttpDelete("config")]
        public async Task<RequestResponse> DeleteConfig()
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.DeletePlaidConfig(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        // =============================================
        // PLAID LINK FLOW
        // =============================================

        /// <summary>
        /// Creates a Plaid link_token for the authenticated user.
        /// Requires MFA to be enabled.
        /// </summary>
        [HttpPost("create-link-token")]
        public async Task<RequestResponse> CreateLinkToken()
        {
            try
            {
                var userId = GetCurrentUserId();

                // Require MFA before allowing Plaid Link
                var mfaCheck = await RequireMfaAsync(userId);
                if (mfaCheck != null) return mfaCheck;

                return await _plaidService.CreateLinkToken(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Exchanges a Plaid public_token for an access_token and creates a funding source.
        /// Requires MFA to be enabled.
        /// </summary>
        [HttpPost("exchange-token")]
        public async Task<RequestResponse> ExchangePublicToken([FromBody] PlaidExchangeTokenRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Require MFA before allowing token exchange
                var mfaCheck = await RequireMfaAsync(userId);
                if (mfaCheck != null) return mfaCheck;

                return await _plaidService.ExchangePublicToken(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        // =============================================
        // PLAID DATA ENDPOINTS
        // =============================================

        /// <summary>
        /// Returns Plaid-specific details for a funding source.
        /// </summary>
        [HttpGet("funding-details")]
        public async Task<RequestResponse> GetFundingDetails([FromQuery] int accountId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var details = await _plaidService.GetPlaidFundingDetails(accountId, userId);

                if (details == null)
                    return new RequestResponse(null, 404, "Plaid funding source not found");

                return new RequestResponse(details, 200, "OK");
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Refreshes balances for a Plaid-linked funding source.
        /// </summary>
        [HttpPost("refresh-balances")]
        public async Task<RequestResponse> RefreshBalances([FromQuery] int plaidItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.RefreshBalances(plaidItemId, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Returns recent transactions for a Plaid-linked funding source.
        /// </summary>
        [HttpGet("transactions")]
        public async Task<RequestResponse> GetTransactions([FromQuery] int plaidItemId, [FromQuery] int days = 30)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.GetTransactions(plaidItemId, userId, days);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Returns ACH auth data for a Plaid-linked funding source.
        /// </summary>
        [HttpGet("auth")]
        public async Task<RequestResponse> GetAuth([FromQuery] int plaidItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.GetAuth(plaidItemId, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Returns identity info for a Plaid-linked funding source.
        /// </summary>
        [HttpGet("identity")]
        public async Task<RequestResponse> GetIdentity([FromQuery] int plaidItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.GetIdentity(plaidItemId, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        /// <summary>
        /// Removes a Plaid item and deactivates the funding source.
        /// </summary>
        [HttpDelete("remove-item")]
        public async Task<RequestResponse> RemoveItem([FromQuery] int plaidItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.RemoveItem(plaidItemId, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }
    }
}
