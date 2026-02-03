using egibi_api.MarketData.Models;

namespace egibi_api.MarketData.Repositories
{
    /// <summary>
    /// Data access layer for OHLC candles stored in QuestDB.
    /// Handles reads (PG wire), writes (ILP), and coverage queries.
    /// </summary>
    public interface IOhlcRepository
    {
        // ============================================
        // COVERAGE QUERIES
        // ============================================

        /// <summary>
        /// Check what data exists for a specific (symbol, source, interval).
        /// Returns earliest/latest timestamps and candle count.
        /// </summary>
        Task<CoverageInfo> GetCoverageAsync(string symbol, string source, string interval);

        /// <summary>
        /// List all available (source, interval) combinations for a symbol,
        /// with date ranges and counts. Used to populate UI dropdowns.
        /// </summary>
        Task<List<SourceSummary>> GetSourceSummariesAsync(string symbol);

        /// <summary>
        /// List all distinct symbols that have data stored.
        /// </summary>
        Task<List<string>> GetAvailableSymbolsAsync();

        // ============================================
        // READ
        // ============================================

        /// <summary>
        /// Query candles for a specific (symbol, source, interval) and date range.
        /// Results are ordered by timestamp ascending.
        /// </summary>
        Task<List<Candle>> GetCandlesAsync(string symbol, string source, string interval,
            DateTime from, DateTime to);

        // ============================================
        // WRITE
        // ============================================

        /// <summary>
        /// Write candles to QuestDB via ILP. The DEDUP UPSERT on the table
        /// prevents duplicates — safe to write overlapping ranges.
        /// </summary>
        Task<int> WriteCandlesAsync(IEnumerable<Candle> candles);

        // ============================================
        // ADMIN
        // ============================================

        /// <summary>
        /// Initialize the ohlc table if it doesn't exist.
        /// Called on application startup.
        /// </summary>
        Task EnsureTableExistsAsync();

        /// <summary>
        /// List all tables in QuestDB (diagnostic).
        /// </summary>
        Task<List<string>> ListTablesAsync();
    }
}
