using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarketsController : ControllerBase
    {
        private readonly MarketsService _marketsService;

        public MarketsController(MarketsService marketsService)
        {
            _marketsService = marketsService;
        }

        [HttpGet("get-markets")]
        public async Task<RequestResponse> GetMarkets()
        {
            return await _marketsService.GetMarkets();
        }

        [HttpGet("get-market")]
        public async Task<RequestResponse> GetMarket(int id)
        {
            return await _marketsService.GetMarket(id);
        }

        [HttpPost("save-market")]
        public async Task<RequestResponse> SaveMarket(Market market)
        {
            return await _marketsService.SaveMarket(market);
        }

        [HttpDelete("delete-markets")]
        public async Task<RequestResponse> DeleteMarkets(List<int> ids)
        {
            return await _marketsService.DeleteMarkets(ids);
        }

        [HttpDelete("delete-market")]
        public async Task<RequestResponse> DeleteMarket(int id)
        {
            return await _marketsService.DeleteMarket(id);
        }

    }
}
