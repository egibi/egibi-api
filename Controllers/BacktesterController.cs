#nullable disable
using egibi_api.Services;
using EgibiCoreLibrary;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BacktesterController : ControllerBase
    {
        private readonly BacktesterService _backtesterService;

        public BacktesterController(BacktesterService backtesterService)
        {
            _backtesterService = backtesterService;
        }

        [HttpGet("get-backtests")]
        public async Task<RequestResponse> GetBacktests()
        {
            return null;
        }

        [HttpGet("get-backtest")]
        public async Task<RequestResponse> GetBacktest(int backtestId)
        {
            return null;
        }

        [HttpPost("create-backtest")]
        public async Task<RequestResponse> CreateBacktest()
        {
            return null;
        }

        [HttpPost("run-backtest")]
        public async Task<RequestResponse> RunBacktest()
        {
            return null;
        }

        [HttpDelete("delete-backtest")]
        public async Task<RequestResponse> DeleteBacktest(int id)
        {
            return null;
        }
    }
}
