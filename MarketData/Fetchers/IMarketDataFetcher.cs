using egibi_api.MarketData.Models;

namespace egibi_api.MarketData.Fetchers
{
    /// <summary>
    /// Interface for fetching historical OHLC data from an external source.
    /// Each exchange or data vendor implements this interface.
    /// 
    /// The MarketDataService discovers registered fetchers by their SourceName
    /// and delegates gap-filling to the appropriate one.
    /// </summary>
    public interface IMarketDataFetcher
    {
        /// <summary>
        /// The canonical source identifier stored in the QuestDB 'source' column.
        /// Must be lowercase, no spaces (e.g., "binance", "coinbase", "alphavantage").
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// Human-readable display name for the UI (e.g., "Binance US", "Coinbase Pro").
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Whether this fetcher can retrieve historical data on demand.
        /// Some sources (e.g., file imports) are not fetchable — they only exist
        /// if data was previously imported.
        /// </summary>
        bool CanFetchOnDemand { get; }

        /// <summary>
        /// Supported intervals for this source.
        /// Used to validate requests before making API calls.
        /// </summary>
        IReadOnlyList<string> SupportedIntervals { get; }

        /// <summary>
        /// Fetch historical candles from the external source.
        /// 
        /// The implementation should handle pagination internally —
        /// most exchange APIs have a per-request limit (e.g., Binance returns 
        /// max 1000 candles per call), so the fetcher pages through until
        /// the full range is covered.
        /// 
        /// The returned candles must have Symbol, Source, Interval, and Timestamp set.
        /// </summary>
        /// <param name="symbol">Trading pair (e.g., "BTCUSD", "BTC-USD" — normalize as needed)</param>
        /// <param name="interval">Candle interval (e.g., "1h", "1d")</param>
        /// <param name="from">Start of requested range (inclusive, UTC)</param>
        /// <param name="to">End of requested range (inclusive, UTC)</param>
        /// <param name="cancellationToken">Cancellation token for long-running fetches</param>
        /// <returns>List of candles ordered by timestamp ascending</returns>
        Task<List<Candle>> FetchCandlesAsync(
            string symbol,
            string interval,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);
    }
}
