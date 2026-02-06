using System.Text.Json;
using egibi_api.Data;
using egibi_api.Models.Strategy;
using egibi_api.MarketData.Models;
using egibi_api.MarketData.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace egibi_api.Services.Backtesting;

/// <summary>
/// Orchestrates backtest execution: loads the strategy config, fetches OHLC data
/// via IMarketDataService (cache-first with auto-fetch from exchanges), verifies
/// data coverage, and runs the SimpleBacktestEngine.
/// </summary>
public class BacktestExecutionService
{
    private readonly EgibiDbContext _db;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<BacktestExecutionService> _logger;

    public BacktestExecutionService(
        EgibiDbContext db,
        IMarketDataService marketDataService,
        ILogger<BacktestExecutionService> logger)
    {
        _db = db;
        _marketDataService = marketDataService;
        _logger = logger;
    }


    // ═══════════════════════════════════════════════════════
    //  EXECUTE BACKTEST
    // ═══════════════════════════════════════════════════════

    public async Task<BacktestResultDto> RunBacktestAsync(BacktestRequestDto request)
    {
        // 1. Load the strategy
        var strategy = await _db.Strategies.FindAsync(request.StrategyId);
        if (strategy == null)
            throw new KeyNotFoundException($"Strategy {request.StrategyId} not found.");

        if (string.IsNullOrEmpty(strategy.RulesConfiguration))
            throw new InvalidOperationException("Strategy has no rules configuration. It may be a code-only strategy.");

        var config = JsonSerializer.Deserialize<StrategyConfigurationDto>(strategy.RulesConfiguration)
            ?? throw new InvalidOperationException("Failed to deserialize strategy rules configuration.");

        // Apply any overrides from the request
        if (!string.IsNullOrEmpty(request.Symbol)) config.Symbol = request.Symbol;
        if (!string.IsNullOrEmpty(request.DataSource)) config.DataSource = request.DataSource;
        if (!string.IsNullOrEmpty(request.Interval)) config.Interval = request.Interval;

        // 2. Fetch OHLC data via MarketDataService (cache-first, auto-fetches gaps)
        var (candles, warnings) = await FetchAndVerifyDataAsync(
            config.Symbol, config.DataSource, config.Interval,
            request.StartDate, request.EndDate);

        if (candles.Count == 0)
        {
            return new BacktestResultDto
            {
                InitialCapital = request.InitialCapital,
                FinalCapital = request.InitialCapital,
                Symbol = config.Symbol,
                DataSource = config.DataSource,
                Interval = config.Interval,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Warnings = warnings
            };
        }

        // 3. Run the engine
        var engine = new SimpleBacktestEngine();
        var result = engine.Execute(config, candles, request.InitialCapital, request.StartDate, request.EndDate);

        // Merge data-fetch warnings into result
        result.Warnings.InsertRange(0, warnings);

        // 4. Save backtest result to database
        await SaveBacktestRecordAsync(request, result, strategy.Name);

        return result;
    }


    // ═══════════════════════════════════════════════════════
    //  DATA VERIFICATION
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Check data availability for a backtest without running it.
    /// Returns coverage info so the UI can show what data exists
    /// and what would need to be fetched.
    /// </summary>
    public async Task<DataVerificationResult> VerifyDataAsync(
        string symbol, string source, string interval,
        DateTime startDate, DateTime endDate)
    {
        var result = new DataVerificationResult
        {
            Symbol = symbol,
            Source = source,
            Interval = interval,
            RequestedFrom = startDate,
            RequestedTo = endDate
        };

        // Check existing coverage
        var coverage = await _marketDataService.GetCoverageAsync(symbol, source, interval);
        result.StoredCandleCount = coverage.CandleCount;
        result.StoredFrom = coverage.Earliest;
        result.StoredTo = coverage.Latest;

        // Check if coverage fully covers the requested range
        if (coverage.FullyCovers(startDate, endDate))
        {
            result.Status = DataVerificationStatus.FullyCovered;
            result.Message = $"Data fully available: {coverage.CandleCount:N0} candles " +
                             $"from {coverage.Earliest:yyyy-MM-dd} to {coverage.Latest:yyyy-MM-dd}.";
        }
        else if (coverage.IsEmpty)
        {
            // Check if a fetcher exists for this source
            var fetchers = _marketDataService.GetRegisteredFetchers();
            var hasFetcher = fetchers.Any(f =>
                f.SourceName.Equals(source, StringComparison.OrdinalIgnoreCase) && f.CanFetchOnDemand);

            result.Status = hasFetcher
                ? DataVerificationStatus.FetchRequired
                : DataVerificationStatus.NoData;
            result.CanAutoFetch = hasFetcher;
            result.Message = hasFetcher
                ? $"No stored data. {source} fetcher available — data will be fetched automatically when the backtest runs."
                : $"No stored data for {symbol} from {source} ({interval}). " +
                  "Import data via the Data Manager before running a backtest.";
        }
        else
        {
            // Partial coverage
            var fetchers = _marketDataService.GetRegisteredFetchers();
            var hasFetcher = fetchers.Any(f =>
                f.SourceName.Equals(source, StringComparison.OrdinalIgnoreCase) && f.CanFetchOnDemand);

            result.Status = hasFetcher
                ? DataVerificationStatus.PartialWithFetch
                : DataVerificationStatus.PartialCoverage;
            result.CanAutoFetch = hasFetcher;

            var gaps = new List<string>();
            if (startDate < coverage.Earliest)
                gaps.Add($"before {coverage.Earliest:yyyy-MM-dd}");
            if (endDate > coverage.Latest)
                gaps.Add($"after {coverage.Latest:yyyy-MM-dd}");

            result.Message = hasFetcher
                ? $"Partial data ({coverage.CandleCount:N0} candles, {coverage.Earliest:yyyy-MM-dd} to {coverage.Latest:yyyy-MM-dd}). " +
                  $"Gaps {string.Join(" and ", gaps)} will be fetched automatically."
                : $"Partial data ({coverage.CandleCount:N0} candles, {coverage.Earliest:yyyy-MM-dd} to {coverage.Latest:yyyy-MM-dd}). " +
                  $"Missing data {string.Join(" and ", gaps)}. Import via Data Manager for full coverage.";
        }

        // Estimate expected candle count
        var duration = Intervals.GetDuration(interval);
        if (duration.HasValue && duration.Value.TotalSeconds > 0)
        {
            result.ExpectedCandleCount = (long)((endDate - startDate).TotalSeconds / duration.Value.TotalSeconds);
        }

        return result;
    }


    // ═══════════════════════════════════════════════════════
    //  DATA COVERAGE (delegates to IMarketDataService)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Returns what OHLC data is available, grouped by symbol/source/interval.
    /// Used by the UI to populate data source dropdowns.
    /// </summary>
    public async Task<List<DataCoverageDto>> GetDataCoverageAsync(string? symbol = null)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            // Get all symbols, then summaries for each
            var symbols = await _marketDataService.GetAvailableSymbolsAsync();
            var all = new List<DataCoverageDto>();
            foreach (var sym in symbols)
            {
                var summaries = await _marketDataService.GetSourceSummariesAsync(sym);
                all.AddRange(summaries.Select(s => new DataCoverageDto
                {
                    Symbol = sym,
                    Source = s.Source,
                    Interval = s.Interval,
                    FromDate = s.Earliest,
                    ToDate = s.Latest,
                    CandleCount = s.CandleCount
                }));
            }
            return all;
        }

        var sources = await _marketDataService.GetSourceSummariesAsync(symbol);
        return sources.Select(s => new DataCoverageDto
        {
            Symbol = symbol,
            Source = s.Source,
            Interval = s.Interval,
            FromDate = s.Earliest,
            ToDate = s.Latest,
            CandleCount = s.CandleCount
        }).ToList();
    }

    /// <summary>
    /// Returns distinct symbols available in QuestDB.
    /// </summary>
    public async Task<List<string>> GetAvailableSymbolsAsync()
    {
        return await _marketDataService.GetAvailableSymbolsAsync();
    }


    // ═══════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Fetches data via IMarketDataService (which checks QuestDB first, then
    /// auto-fetches gaps from the exchange if a fetcher is registered).
    /// Returns the candles mapped to OhlcCandle and any data-quality warnings.
    /// </summary>
    private async Task<(List<OhlcCandle> Candles, List<string> Warnings)> FetchAndVerifyDataAsync(
        string symbol, string source, string interval, DateTime start, DateTime end)
    {
        var warnings = new List<string>();

        // Use MarketDataService — this handles cache + auto-fetch from exchanges
        var dataRequest = new MarketDataRequest
        {
            Symbol = symbol,
            Source = source,
            Interval = interval,
            From = start,
            To = end
        };

        MarketDataResult dataResult;
        try
        {
            dataResult = await _marketDataService.GetCandlesAsync(dataRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch market data for {Symbol}/{Source}/{Interval}", symbol, source, interval);
            warnings.Add($"Failed to retrieve market data: {ex.Message}");
            return (new List<OhlcCandle>(), warnings);
        }

        // Log fetch details
        _logger.LogInformation(
            "MarketDataService returned {Total} candles for {Symbol}/{Source}/{Interval} " +
            "({Cached} cached, {Fetched} freshly fetched) from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            dataResult.Candles.Count, symbol, source, interval,
            dataResult.CachedCount, dataResult.FetchedCount, start, end);

        if (dataResult.Candles.Count == 0)
        {
            warnings.Add(
                $"No OHLC data found for {symbol} from {source} ({interval}) " +
                $"between {start:yyyy-MM-dd} and {end:yyyy-MM-dd}. " +
                "Try fetching data first via the Data Manager.");
            return (new List<OhlcCandle>(), warnings);
        }

        // Add info about data sourcing
        if (dataResult.FetchedCount > 0)
        {
            warnings.Add(
                $"Fetched {dataResult.FetchedCount:N0} new candles from {source} " +
                $"(plus {dataResult.CachedCount:N0} from cache).");
        }

        // Check for potential coverage gaps
        var actualFrom = dataResult.Candles.Min(c => c.Timestamp);
        var actualTo = dataResult.Candles.Max(c => c.Timestamp);

        var duration = Intervals.GetDuration(interval);
        if (duration.HasValue && duration.Value.TotalSeconds > 0)
        {
            var expectedCount = (long)((end - start).TotalSeconds / duration.Value.TotalSeconds);
            var actualCount = dataResult.Candles.Count;
            var coverageRatio = expectedCount > 0 ? (double)actualCount / expectedCount : 0;

            if (coverageRatio < 0.9 && expectedCount > 10)
            {
                warnings.Add(
                    $"Data coverage is {coverageRatio:P0} ({actualCount:N0} of ~{expectedCount:N0} expected candles). " +
                    "Results may not reflect the full date range.");
            }
        }

        if (actualFrom > start.AddHours(1))
        {
            warnings.Add($"Data starts at {actualFrom:yyyy-MM-dd HH:mm} UTC, " +
                         $"which is after the requested start of {start:yyyy-MM-dd}.");
        }

        if (actualTo < end.AddHours(-1))
        {
            warnings.Add($"Data ends at {actualTo:yyyy-MM-dd HH:mm} UTC, " +
                         $"which is before the requested end of {end:yyyy-MM-dd}.");
        }

        // Map Candle (double) → OhlcCandle (decimal) for the backtest engine
        var candles = dataResult.Candles
            .OrderBy(c => c.Timestamp)
            .Select(c => new OhlcCandle
            {
                Timestamp = c.Timestamp,
                Open = (decimal)c.Open,
                High = (decimal)c.High,
                Low = (decimal)c.Low,
                Close = (decimal)c.Close,
                Volume = (decimal)c.Volume
            })
            .ToList();

        return (candles, warnings);
    }

    private async Task SaveBacktestRecordAsync(BacktestRequestDto request, BacktestResultDto result, string strategyName)
    {
        try
        {
            var backtest = new Data.Entities.Backtest
            {
                Name = $"{strategyName} - {result.Symbol} ({result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd})",
                StrategyId = request.StrategyId,
                BacktestStatusId = 3, // Completed
                StartDate = result.StartDate,
                EndDate = result.EndDate,
                InitialCapital = result.InitialCapital,
                FinalCapital = result.FinalCapital,
                TotalReturnPct = result.TotalReturnPct,
                TotalTrades = result.TotalTrades,
                WinRate = result.WinRate,
                MaxDrawdownPct = result.MaxDrawdownPct,
                SharpeRatio = result.SharpeRatio,
                ResultJson = JsonSerializer.Serialize(result),
                ExecutedAt = DateTime.UtcNow
            };

            _db.Backtests.Add(backtest);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save backtest record. Results are still returned.");
            result.Warnings.Add("Backtest completed but the result could not be saved to the database.");
        }
    }
}


// ═══════════════════════════════════════════════════════════
//  DATA VERIFICATION MODELS
// ═══════════════════════════════════════════════════════════

public enum DataVerificationStatus
{
    /// <summary>Stored data fully covers the requested range.</summary>
    FullyCovered,

    /// <summary>Partial data exists; a fetcher can fill the gaps automatically.</summary>
    PartialWithFetch,

    /// <summary>Partial data exists but no auto-fetcher is available.</summary>
    PartialCoverage,

    /// <summary>No data exists but a fetcher can retrieve it.</summary>
    FetchRequired,

    /// <summary>No data and no way to get it automatically.</summary>
    NoData
}

public class DataVerificationResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime RequestedFrom { get; set; }
    public DateTime RequestedTo { get; set; }

    public DataVerificationStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Whether a fetcher can auto-fill missing data.</summary>
    public bool CanAutoFetch { get; set; }

    /// <summary>How many candles are already stored in QuestDB.</summary>
    public long StoredCandleCount { get; set; }

    /// <summary>Earliest stored candle timestamp (null if none).</summary>
    public DateTime? StoredFrom { get; set; }

    /// <summary>Latest stored candle timestamp (null if none).</summary>
    public DateTime? StoredTo { get; set; }

    /// <summary>Approximate number of candles expected for the full range.</summary>
    public long ExpectedCandleCount { get; set; }
}