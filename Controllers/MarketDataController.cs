#nullable disable
using egibi_api.MarketData.Models;
using egibi_api.MarketData.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarketDataController : ControllerBase
    {
        private readonly IMarketDataService _marketDataService;

        public MarketDataController(IMarketDataService marketDataService)
        {
            _marketDataService = marketDataService;
        }

        // ============================================
        // DATA ACCESS — Cache-First
        // ============================================

        /// <summary>
        /// Get candles for a symbol/source/interval/date range.
        /// Automatically fetches from the exchange if gaps exist
        /// and the source supports on-demand fetching.
        /// </summary>
        [HttpPost("get-candles")]
        public async Task<RequestResponse> GetCandles([FromBody] MarketDataRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Symbol))
                    return new RequestResponse(null, 400, "Symbol is required.");
                if (string.IsNullOrWhiteSpace(request.Source))
                    return new RequestResponse(null, 400, "Source is required.");
                if (string.IsNullOrWhiteSpace(request.Interval))
                    return new RequestResponse(null, 400, "Interval is required.");
                if (request.From >= request.To)
                    return new RequestResponse(null, 400, "From must be before To.");

                var result = await _marketDataService.GetCandlesAsync(request);
                return new RequestResponse(result, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Error retrieving market data.", new ResponseError(ex));
            }
        }

        // ============================================
        // DISCOVERY — What's Available?
        // ============================================

        /// <summary>
        /// Get all symbols that have stored data.
        /// </summary>
        [HttpGet("get-symbols")]
        public async Task<RequestResponse> GetSymbols()
        {
            try
            {
                var symbols = await _marketDataService.GetAvailableSymbolsAsync();
                return new RequestResponse(symbols, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Error retrieving symbols.", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Get all available (source, interval) combinations for a symbol,
        /// with date ranges and candle counts.
        /// </summary>
        [HttpGet("get-source-summaries")]
        public async Task<RequestResponse> GetSourceSummaries([FromQuery] string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                    return new RequestResponse(null, 400, "Symbol is required.");

                var summaries = await _marketDataService.GetSourceSummariesAsync(symbol);
                return new RequestResponse(summaries, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Error retrieving source summaries.", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Check coverage for a specific (symbol, source, interval).
        /// Returns earliest/latest timestamps and candle count.
        /// </summary>
        [HttpGet("get-coverage")]
        public async Task<RequestResponse> GetCoverage(
            [FromQuery] string symbol,
            [FromQuery] string source,
            [FromQuery] string interval)
        {
            try
            {
                var coverage = await _marketDataService.GetCoverageAsync(symbol, source, interval);
                return new RequestResponse(coverage, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Error retrieving coverage.", new ResponseError(ex));
            }
        }

        /// <summary>
        /// List all registered data fetchers (exchanges/vendors).
        /// Shows which sources can fetch on demand vs. import-only.
        /// </summary>
        [HttpGet("get-fetchers")]
        public RequestResponse GetFetchers()
        {
            try
            {
                var fetchers = _marketDataService.GetRegisteredFetchers();
                return new RequestResponse(fetchers, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Error retrieving fetchers.", new ResponseError(ex));
            }
        }

        // ============================================
        // IMPORT — Manual Data Ingestion
        // ============================================

        /// <summary>
        /// Import candles directly (e.g., from a parsed CSV).
        /// DEDUP on the QuestDB table prevents duplicates.
        /// </summary>
        [HttpPost("import-candles")]
        public async Task<RequestResponse> ImportCandles([FromBody] List<Candle> candles)
        {
            try
            {
                if (candles == null || candles.Count == 0)
                    return new RequestResponse(null, 400, "No candles provided.");

                var count = await _marketDataService.ImportCandlesAsync(candles);
                return new RequestResponse(new { imported = count }, 200, $"Imported {count} candles.");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Error importing candles.", new ResponseError(ex));
            }
        }
    }
}
