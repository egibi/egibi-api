#nullable disable
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class ExchangeAccounts : ControllerBase
    {

        public readonly ExchangeAccountsService _exchangeAccountsService;

        [HttpGet("get-exchange-accounts")]
        public async Task<RequestResponse> GetExchangeAccounts()
        {
            return await _exchangeAccountsService.GetExchangeAccounts();
        }

        [HttpGet("get-exchange-account")]
        public async Task<RequestResponse> GetExcahngeAccount(int id)
        {
            return await _exchangeAccountsService.GetExchangeAccount(id);
        }
    }
}
