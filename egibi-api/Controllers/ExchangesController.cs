using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Exchange = egibi_api.Data.Entities.Exchange;

namespace egibi_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ExchangesController : ControllerBase
    {
        private readonly ExchangesService _exchangesService;

        public ExchangesController(ExchangesService exchangesService)
        {
            _exchangesService = exchangesService;
        }

        [HttpGet("get-exchanges")]
        public async Task<RequestResponse> GetExchanges()
        {
            return await _exchangesService.GetExchanges();
        }

        [HttpGet("get-exchange")]
        public async Task<RequestResponse> GetExchange(int id)
        {
            return await _exchangesService.GetExchange(id);
        }

        [HttpGet("get-exchange-types")]
        public async Task<RequestResponse> GetExchangeTypes()
        {
            return await _exchangesService.GetExchangeTypes();
        }

        [HttpPost("save-exchange")]
        public async Task<RequestResponse> SaveExchange(Exchange exchange)
        {
            return await _exchangesService.SaveExchange(exchange);
        }

        [HttpDelete("delete-exchanges")]
        public async Task<RequestResponse> DeleteExchanges(List<int> ids)
        {
            return await _exchangesService.DeleteExchanges(ids);
        }

        [HttpDelete("delete-exchange")]
        public async Task<RequestResponse> DeleteExchange(int id)
        {
            return await _exchangesService.DeleteExchange(id);
        }
    }
}
