using egibi_api.MarketData.Models;

namespace egibi_api.MarketData.Services
{
    /// <summary>
    /// Orchestrator for all market data access. Implements the cache-first pattern:
    /// 
    /// 1. Check QuestDB for existing data (coverage check)
    /// 2. Identify gaps in the requested range
    /// 3. Fill gaps by fetching from the appropriate exchange/source
    /// 4. Store fetched data in QuestDB
    /// 5. Return the complete dataset from QuestDB
    /// 
    /// This is the single entry point that the backtester, charts, and
    /// Data Manager UI all call. Consumers never talk to QuestDB or
    /// exchange APIs directly.
    /// </summary>
    public interface IMarketDataService
    {
        // ============================================
        // PRIMARY — Cache-First Data Access
        // ============================================

        /// <summary>
        /// Get candles for a request. Checks local storage first,
        /// fetches gaps from the source if it supports on-demand fetching,
        /// then returns the full dataset.
        /// </summary>
        Task<MarketDataResult> GetCandlesAsync(MarketDataRequest request,
            CancellationToken cancellationToken = default);

        // ============================================
        // DISCOVERY — What Data Is Available?
        // ============================================

        /// <summary>
        /// Get all (source, interval) combinations available for a symbol,
        /// with date ranges and counts. For populating UI dropdowns.
        /// </summary>
        Task<List<SourceSummary>> GetSourceSummariesAsync(string symbol);

        /// <summary>
        /// Get all symbols that have any data stored.
        /// </summary>
        Task<List<string>> GetAvailableSymbolsAsync();

        /// <summary>
        /// Check what data exists for a specific (symbol, source, interval).
        /// </summary>
        Task<CoverageInfo> GetCoverageAsync(string symbol, string source, string interval);

        // ============================================
        // IMPORT — Manual Data Ingestion
        // ============================================

        /// <summary>
        /// Directly import candles (e.g., from a CSV upload).
        /// Writes to QuestDB; DEDUP prevents duplicates.
        /// </summary>
        Task<int> ImportCandlesAsync(IEnumerable<Candle> candles);

        // ============================================
        // FETCHERS — Source Registry
        // ============================================

        /// <summary>
        /// List all registered data fetchers and whether they support
        /// on-demand fetching. For the Data Manager UI.
        /// </summary>
        List<FetcherInfo> GetRegisteredFetchers();
    }

    /// <summary>
    /// Metadata about a registered data fetcher, for UI display.
    /// </summary>
    public class FetcherInfo
    {
        public string SourceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool CanFetchOnDemand { get; set; }
        public IReadOnlyList<string> SupportedIntervals { get; set; } = Array.Empty<string>();
    }
}
