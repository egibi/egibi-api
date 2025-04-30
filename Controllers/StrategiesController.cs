using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StrategiesController : ControllerBase
    {
        private readonly StrategiesService _strategiesService;

        public StrategiesController(StrategiesService strategiesService)
        {
            _strategiesService = strategiesService;
        }

        [HttpGet("get-strategies")]
        public async Task<RequestResponse> GetStrategies()
        {
            return await _strategiesService.GetStrategies();
        }

        [HttpGet("get-strategy")]
        public async Task<RequestResponse> Get(int strategyId)
        {
            return await _strategiesService.GetStrategy(strategyId);
        }

        [HttpPost("save-strategy")]
        public async Task<RequestResponse> SaveStrategy(Strategy strategy)
        {
            return await _strategiesService.SaveStrategy(strategy);
        }

        [HttpDelete("delete-strategies")]
        public async Task<RequestResponse> DeleteStrategies(List<int> strategyIds)
        {
            return await _strategiesService.DeleteStrategies(strategyIds);
        }

        [HttpDelete("delete-strategy")]
        public async Task<RequestResponse> DeleteStrategy(int id)
        {
            return await _strategiesService.DeleteStrategy(id);
        }

        [HttpPost("run-strategy")]
        public async Task<RequestResponse> RunStrategy(int id)
        {
            return null;
        }
    }
}
