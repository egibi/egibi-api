#nullable disable
using System.Security.Claims;
using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace egibi_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ExchangeAccountsController : ControllerBase
    {
        private readonly ExchangeAccountsService _exchangeAccountsService;

        public ExchangeAccountsController(ExchangeAccountsService exchangeAccountsService)
        {
            _exchangeAccountsService = exchangeAccountsService;
        }

        // FIX F-02: Extract authenticated user ID from OIDC claims (same pattern as AccountsController)
        private int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(Claims.Subject);
            return int.Parse(sub ?? throw new UnauthorizedAccessException("No subject claim found"));
        }

        [HttpGet("get-exchange-accounts")]
        public async Task<RequestResponse> GetExchangeAccounts()
        {
            var userId = GetCurrentUserId();
            return await _exchangeAccountsService.GetExchangeAccounts(userId);
        }

        [HttpGet("get-exchange-account")]
        public async Task<RequestResponse> GetExchangeAccount(int id)
        {
            var userId = GetCurrentUserId();
            return await _exchangeAccountsService.GetExchangeAccount(id, userId);
        }

        [HttpPost("save-exchange-account")]
        public async Task<RequestResponse> SaveExchangeAccount([FromBody] ExchangeAccount exchangeAccount)
        {
            var userId = GetCurrentUserId();
            return await _exchangeAccountsService.SaveExchangeAccount(exchangeAccount, userId);
        }

        [HttpDelete("delete-exchange-account")]
        public async Task<RequestResponse> DeleteExchangeAccount(int id)
        {
            var userId = GetCurrentUserId();
            return await _exchangeAccountsService.DeleteExchangeAccount(id, userId);
        }

        [HttpDelete("delete-exchange-accounts")]
        public async Task<RequestResponse> DeleteExchangeAccounts([FromBody] List<int> ids)
        {
            var userId = GetCurrentUserId();
            return await _exchangeAccountsService.DeleteExchangeAccounts(ids, userId);
        }
    }
}
