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
    public class FundingController : ControllerBase
    {
        private readonly FundingService _fundingService;

        public FundingController(FundingService fundingService)
        {
            _fundingService = fundingService;
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
        // GET PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Returns the user's primary funding source with masked credentials.
        /// Returns 200 with null data if no funding source is configured.
        /// </summary>
        [HttpGet("get-primary")]
        public async Task<RequestResponse> GetPrimary()
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _fundingService.GetPrimaryFundingSource(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to load funding source", new ResponseError(ex));
            }
        }

        // =============================================
        // GET FUNDING PROVIDERS
        // =============================================

        /// <summary>
        /// Returns available funding provider connections for the setup wizard.
        /// </summary>
        [HttpGet("get-providers")]
        public async Task<RequestResponse> GetProviders()
        {
            try
            {
                return await _fundingService.GetFundingProviders();
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to load funding providers", new ResponseError(ex));
            }
        }

        // =============================================
        // SET PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Creates a new account as the primary funding source.
        /// Encrypts credentials and demotes any existing primary.
        /// </summary>
        [HttpPost("set-primary")]
        public async Task<RequestResponse> SetPrimary([FromBody] CreateFundingSourceRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _fundingService.SetPrimaryFundingSource(request, userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to set funding source", new ResponseError(ex));
            }
        }

        // =============================================
        // REMOVE PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Removes the primary funding flag (does not delete the account).
        /// </summary>
        [HttpDelete("remove-primary")]
        public async Task<RequestResponse> RemovePrimary()
        {
            try
            {
                var userId = GetCurrentUserId();
                return await _fundingService.RemovePrimaryFundingSource(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return new RequestResponse(null, 401, "Unauthorized");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to remove funding source", new ResponseError(ex));
            }
        }
    }
}
