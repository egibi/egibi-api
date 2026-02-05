using System.Security.Claims;
using egibi_api.Models;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Account = egibi_api.Data.Entities.Account;
using AccountDetails = egibi_api.Data.Entities.AccountDetails;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly AccountsService _accountsService;

        public AccountsController(AccountsService accountsService)
        {
            _accountsService = accountsService;
        }

        /// <summary>
        /// Returns the current authenticated user's ID from the OIDC claims.
        /// </summary>
        private int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(Claims.Subject);
            return int.Parse(sub ?? throw new UnauthorizedAccessException("No subject claim found"));
        }

        // =============================================
        // ACCOUNT DETAIL (for detail page)
        // =============================================

        /// <summary>
        /// Returns full account detail including connection metadata,
        /// masked credential summary, and fee structure.
        /// </summary>
        [HttpGet("get-account-detail")]
        public async Task<RequestResponse> GetAccountDetail(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _accountsService.GetAccountDetail(id, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to load account detail", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Updates general account properties (name, description, type, active status).
        /// </summary>
        [HttpPut("update-account")]
        public async Task<RequestResponse> UpdateAccount([FromBody] UpdateAccountRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _accountsService.UpdateAccount(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to update account", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Updates encrypted credentials for an account's connection.
        /// Only non-null fields are updated — pass null to keep existing value.
        /// </summary>
        [HttpPut("update-credentials")]
        public async Task<RequestResponse> UpdateCredentials([FromBody] UpdateCredentialsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _accountsService.UpdateAccountCredentials(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to update credentials", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Updates the fee structure for an account.
        /// Creates a new fee record if one doesn't exist.
        /// </summary>
        [HttpPut("update-fees")]
        public async Task<RequestResponse> UpdateFees([FromBody] UpdateAccountFeesRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _accountsService.UpdateAccountFees(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to update fees", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Tests connectivity to the account's exchange API.
        /// Makes an unauthenticated HTTP request to the base URL.
        /// </summary>
        [HttpPost("test-connection")]
        public async Task<RequestResponse> TestConnection([FromQuery] int accountId)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _accountsService.TestAccountConnection(accountId, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to test connection", new ResponseError(ex));
            }
        }

        // =============================================
        // SERVICE CATALOG ACCOUNT CREATION
        // =============================================

        /// <summary>
        /// Creates a new account linked to a service catalog connection.
        /// Encrypts any submitted credentials with the user's DEK and stores
        /// them in UserCredential. Returns an AccountResponse (never plaintext).
        /// </summary>
        [HttpPost("create-account")]
        public async Task<RequestResponse> CreateAccountFromRequest([FromBody] CreateAccountRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _accountsService.CreateAccountFromRequest(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to create account", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Returns all accounts for the current user with connection metadata.
        /// Credentials are returned as masked summaries only.
        /// </summary>
        [HttpGet("get-accounts")]
        public async Task<RequestResponse> GetAccounts()
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _accountsService.GetAccountsForUser(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
        }

        [HttpGet("get-account")]
        public async Task<RequestResponse> GetAccount(int id)
        {
            return await _accountsService.GetAccount(id);
        }

        [HttpGet("get-account-types")]
        public async Task<RequestResponse> GetAccountTypes()
        {
            return await _accountsService.GetAccountTypes();
        }

        // =============================================
        // LEGACY CRUD (retained for backward compatibility)
        // =============================================

        [HttpPost("save-account")]
        public async Task<RequestResponse> SaveAccount([FromBody] Account account)
        {
            return await _accountsService.SaveAccount(account);
        }

        [HttpDelete("delete-accounts")]
        public async Task<RequestResponse> DeleteAccounts(List<int> ids)
        {
            return await _accountsService.DeleteAccounts(ids);
        }

        [HttpDelete("delete-account")]
        public async Task<RequestResponse> DeleteAccount(int id)
        {
            return await _accountsService.DeleteAccount(id);
        }

        // DETAILS
        [HttpPost("save-account-details")]
        public async Task<RequestResponse> SaveAccountDetails(AccountDetails accountDetails)
        {
            return await _accountsService.SaveAccountDetails(accountDetails);
        }
    }
}
