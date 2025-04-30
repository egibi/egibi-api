#nullable disable
using egibi_api.Data.Entities;
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
            return await _backtesterService.GetBacktests();
        }

        [HttpGet("get-backtest")]
        public async Task<RequestResponse> GetBacktest(int backtestId)
        {
            return await _backtesterService.GetBacktest(backtestId);
        }

        [HttpPost("save-backtest")]
        public async Task<RequestResponse> SaveBacktest(Backtest backtest)
        {
            return await _backtesterService.SaveBacktest(backtest);
        }

        [HttpDelete("delete-backtest")]
        public async Task<RequestResponse> DeleteBacktest(int backtestId)
        {
            return await _backtesterService.DeleteBacktest(backtestId);
        }

        [HttpDelete("delete-backtests")]
        public async Task<RequestResponse> DeleteBacktests(List<int> backtestIds)
        {
            return await _backtesterService.DeleteBacktests(backtestIds);
        }

        [HttpGet("get-data-sources")]
        public async Task<RequestResponse> GetDataSources()
        {
            return await _backtesterService.GetDataSources();
        }

        [HttpPost("run-backtest")]
        public async Task<RequestResponse> RunBacktest()
        {
            return null;
        }

        [HttpPost("upload-historical-data-file")]
        public async Task<RequestResponse> UploadHistoricalDataFile()
        {
            return null;
        }
    }
}
