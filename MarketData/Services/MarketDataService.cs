using egibi_api.MarketData.Fetchers;
using egibi_api.MarketData.Models;
using egibi_api.MarketData.Repositories;
using Microsoft.Extensions.Logging;

namespace egibi_api.MarketData.Services
{
    /// <summary>
    /// Implements the cache-first data access pattern.
    /// 
    /// Flow for every data request:
    /// 
    ///   Request("BTC-USD", "binance", "1h", Jan 1 – Mar 31)
    ///       │
    ///       ├─► Coverage check: "We have Jan 1 – Feb 15"
    ///       │
    ///       ├─► Gap identified: Feb 16 – Mar 31
    ///       │
    ///       ├─► BinanceFetcher fetches Feb 16 – Mar 31
    ///       │
    ///       ├─► WriteCandlesAsync stores the new candles
    ///       │
    ///       └─► GetCandlesAsync returns full Jan 1 – Mar 31
    /// 
    /// </summary>
    public class MarketDataService : IMarketDataService
    {
        private readonly IOhlcRepository _repository;
        private readonly Dictionary<string, IMarketDataFetcher> _fetchers;
        private readonly ILogger<MarketDataService> _logger;

        public MarketDataService(
            IOhlcRepository repository,
            IEnumerable<IMarketDataFetcher> fetchers,
            ILogger<MarketDataService> logger)
        {
            _repository = repository;
            _logger = logger;

            // Index fetchers by their SourceName for fast lookup
            _fetchers = fetchers.ToDictionary(f => f.SourceName, StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("MarketDataService initialized with {Count} fetcher(s): {Names}",
                _fetchers.Count,
                string.Join(", ", _fetchers.Keys));
        }

        // ============================================
        // PRIMARY — Cache-First Data Access
        // ============================================

        public async Task<MarketDataResult> GetCandlesAsync(
            MarketDataRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new MarketDataResult
            {
                Symbol = request.Symbol,
                Source = request.Source,
                Interval = request.Interval,
                From = request.From,
                To = request.To
            };

            // Step 1: Check what we already have
            var coverage = await _repository.GetCoverageAsync(
                request.Symbol, request.Source, request.Interval);

            _logger.LogInformation(
                "Coverage for {Symbol}/{Source}/{Interval}: {Count} candles, {Earliest:yyyy-MM-dd} to {Latest:yyyy-MM-dd}",
                request.Symbol, request.Source, request.Interval,
                coverage.CandleCount,
                coverage.Earliest,
                coverage.Latest);

            // Step 2: Identify gaps
            var gaps = IdentifyGaps(request, coverage);

            // Step 3: Fill gaps if a fetcher is available and supports on-demand
            if (gaps.Count > 0)
            {
                if (_fetchers.TryGetValue(request.Source, out var fetcher) && fetcher.CanFetchOnDemand)
                {
                    foreach (var gap in gaps)
                    {
                        _logger.LogInformation(
                            "Filling gap: {From:yyyy-MM-dd HH:mm} to {To:yyyy-MM-dd HH:mm}",
                            gap.From, gap.To);

                        try
                        {
                            var fetched = await fetcher.FetchCandlesAsync(
                                request.Symbol, request.Interval,
                                gap.From, gap.To, cancellationToken);

                            if (fetched.Count > 0)
                            {
                                var written = await _repository.WriteCandlesAsync(fetched);
                                result.FetchedCount += written;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Failed to fetch gap {From} to {To} from {Source}",
                                gap.From, gap.To, request.Source);
                            // Continue with whatever data we have — don't fail the whole request
                        }
                    }
                }
                else if (gaps.Count > 0)
                {
                    _logger.LogInformation(
                        "Gaps exist but no on-demand fetcher for source '{Source}'. Returning stored data only.",
                        request.Source);
                }
            }

            // Step 4: Return the full range from QuestDB (now includes any freshly written data)
            // Small delay to let QuestDB WAL commit the writes
            if (result.FetchedCount > 0)
            {
                await Task.Delay(500, cancellationToken);
            }

            result.Candles = await _repository.GetCandlesAsync(
                request.Symbol, request.Source, request.Interval,
                request.From, request.To);

            result.CachedCount = result.Candles.Count - result.FetchedCount;
            if (result.CachedCount < 0) result.CachedCount = 0;

            _logger.LogInformation(
                "Returning {Total} candles ({Cached} cached, {Fetched} fetched) for {Symbol}/{Source}/{Interval}",
                result.Candles.Count, result.CachedCount, result.FetchedCount,
                request.Symbol, request.Source, request.Interval);

            return result;
        }

        // ============================================
        // DISCOVERY
        // ============================================

        public async Task<List<SourceSummary>> GetSourceSummariesAsync(string symbol)
        {
            return await _repository.GetSourceSummariesAsync(symbol);
        }

        public async Task<List<string>> GetAvailableSymbolsAsync()
        {
            return await _repository.GetAvailableSymbolsAsync();
        }

        public async Task<CoverageInfo> GetCoverageAsync(string symbol, string source, string interval)
        {
            return await _repository.GetCoverageAsync(symbol, source, interval);
        }

        // ============================================
        // IMPORT
        // ============================================

        public async Task<int> ImportCandlesAsync(IEnumerable<Candle> candles)
        {
            return await _repository.WriteCandlesAsync(candles);
        }

        // ============================================
        // FETCHER REGISTRY
        // ============================================

        public List<FetcherInfo> GetRegisteredFetchers()
        {
            return _fetchers.Values.Select(f => new FetcherInfo
            {
                SourceName = f.SourceName,
                DisplayName = f.DisplayName,
                CanFetchOnDemand = f.CanFetchOnDemand,
                SupportedIntervals = f.SupportedIntervals
            }).ToList();
        }

        // ============================================
        // GAP DETECTION
        // ============================================

        /// <summary>
        /// Compare the requested range against stored coverage
        /// and return the time ranges that need fetching.
        /// 
        /// Handles three cases:
        ///   1. No data at all → one gap covering the full request
        ///   2. Data starts after request start → gap at the head
        ///   3. Data ends before request end → gap at the tail
        /// 
        /// Head and tail gaps can both exist simultaneously.
        /// Internal gaps (holes in the middle) are not detected here —
        /// the DEDUP upsert on write means re-fetching an overlapping
        /// range is safe and idempotent.
        /// </summary>
        private static List<DataGap> IdentifyGaps(MarketDataRequest request, CoverageInfo coverage)
        {
            var gaps = new List<DataGap>();

            // Case 1: No data at all
            if (coverage.IsEmpty)
            {
                gaps.Add(new DataGap { From = request.From, To = request.To });
                return gaps;
            }

            // Case 2: Head gap — request starts before our earliest data
            if (request.From < coverage.Earliest!.Value)
            {
                gaps.Add(new DataGap
                {
                    From = request.From,
                    To = coverage.Earliest.Value.AddSeconds(-1)
                });
            }

            // Case 3: Tail gap — request ends after our latest data
            if (request.To > coverage.Latest!.Value)
            {
                gaps.Add(new DataGap
                {
                    From = coverage.Latest.Value.AddSeconds(1),
                    To = request.To
                });
            }

            return gaps;
        }
    }
}
