using System.Security.Claims;
using egibi_api.Models;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PlaidController : ControllerBase
    {
        private readonly PlaidService _plaidService;

        public PlaidController(PlaidService plaidService)
        {
            _plaidService = plaidService;
        }

        private int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(Claims.Subject);
            return int.Parse(sub ?? throw new UnauthorizedAccessException("No subject claim found"));
        }

        // =============================================
        // PLAID CONFIG MANAGEMENT
        // =============================================

        /// <summary>
        /// Returns whether the current user has Plaid credentials configured.
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
        /// Saves (creates or updates) the user's Plaid developer credentials.
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
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to save Plaid configuration", new ResponseError(ex));
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
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to delete Plaid configuration", new ResponseError(ex));
            }
        }

        // =============================================
        // CREATE LINK TOKEN
        // =============================================

        [HttpPost("create-link-token")]
        public async Task<RequestResponse> CreateLinkToken()
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.CreateLinkToken(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to create link token", new ResponseError(ex));
            }
        }

        // =============================================
        // EXCHANGE PUBLIC TOKEN
        // =============================================

        [HttpPost("exchange-token")]
        public async Task<RequestResponse> ExchangeToken([FromBody] PlaidExchangeTokenRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _plaidService.ExchangePublicToken(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to exchange token", new ResponseError(ex));
            }
        }

        // =============================================
        // GET PLAID FUNDING DETAILS
        // =============================================

        [HttpGet("funding-details")]
        public async Task<RequestResponse> GetFundingDetails(int accountId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var details = await _plaidService.GetPlaidFundingDetails(accountId, userId);
                return new RequestResponse(details, 200, "OK");
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to get funding details", new ResponseError(ex));
            }
        }

        // =============================================
        // REFRESH BALANCES
        // =============================================

        [HttpPost("refresh-balances")]
        public async Task<RequestResponse> RefreshBalances(int plaidItemId)
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
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to refresh balances", new ResponseError(ex));
            }
        }

        // =============================================
        // GET TRANSACTIONS
        // =============================================

        [HttpGet("transactions")]
        public async Task<RequestResponse> GetTransactions(int plaidItemId, int days = 30)
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
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to get transactions", new ResponseError(ex));
            }
        }

        // =============================================
        // GET AUTH (ACH NUMBERS)
        // =============================================

        [HttpGet("auth")]
        public async Task<RequestResponse> GetAuth(int plaidItemId)
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
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to get auth data", new ResponseError(ex));
            }
        }

        // =============================================
        // GET IDENTITY
        // =============================================

        [HttpGet("identity")]
        public async Task<RequestResponse> GetIdentity(int plaidItemId)
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
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to get identity data", new ResponseError(ex));
            }
        }

        // =============================================
        // REMOVE ITEM
        // =============================================

        [HttpDelete("remove-item")]
        public async Task<RequestResponse> RemoveItem(int plaidItemId)
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
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to remove Plaid item", new ResponseError(ex));
            }
        }
    }
}
