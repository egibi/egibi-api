#nullable disable
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("get-exchange-accounts")]
        public async Task<RequestResponse> GetExchangeAccounts()
        {
            return await _exchangeAccountsService.GetExchangeAccounts();
        }

        [HttpGet("get-exchange-account")]
        public async Task<RequestResponse> GetExchangeAccount(int id)
        {
            return await _exchangeAccountsService.GetExchangeAccount(id);
        }
    }
}
