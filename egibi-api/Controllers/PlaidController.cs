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
        // CREATE LINK TOKEN
        // =============================================

        /// <summary>
        /// Creates a Plaid link_token for initializing Plaid Link on the frontend.
        /// The link_token expires after 4 hours.
        /// </summary>
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

        /// <summary>
        /// Exchanges a Plaid public_token for an access_token and creates
        /// the funding source account with linked bank details.
        /// </summary>
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

        /// <summary>
        /// Returns Plaid-specific details for a funding account.
        /// </summary>
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

        /// <summary>
        /// Refreshes balance data for all accounts in a Plaid item.
        /// </summary>
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

        /// <summary>
        /// Fetches recent transactions for a Plaid item.
        /// </summary>
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

        /// <summary>
        /// Retrieves ACH account/routing numbers for a Plaid item.
        /// </summary>
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

        /// <summary>
        /// Retrieves identity info for a Plaid item.
        /// </summary>
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

        /// <summary>
        /// Revokes the Plaid access_token and removes the PlaidItem.
        /// The Egibi Account is NOT deleted — just the Plaid link.
        /// </summary>
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
