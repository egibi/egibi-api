using System.Text.Json;
using egibi_api.MarketData.Models;
using Microsoft.Extensions.Logging;

namespace egibi_api.MarketData.Fetchers
{
    /// <summary>
    /// Fetches historical OHLC candles from the Binance US REST API.
    /// 
    /// Endpoint: GET /api/v3/klines
    /// Docs: https://docs.binance.us/#get-candlestick-data
    /// 
    /// Binance returns max 1000 candles per request.
    /// This fetcher pages through automatically.
    /// 
    /// Rate limit: 1200 request weight / minute.
    /// klines endpoint costs 2 weight per call, so ~600 calls/min max.
    /// We add a small delay between pages to be safe.
    /// </summary>
    public class BinanceFetcher : IMarketDataFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BinanceFetcher> _logger;

        private const string BaseUrl = "https://api.binance.us";
        private const int MaxCandlesPerRequest = 1000;
        private const int PageDelayMs = 100; // Polite rate limiting

        public string SourceName => "binance";
        public string DisplayName => "Binance US";
        public bool CanFetchOnDemand => true;

        public IReadOnlyList<string> SupportedIntervals { get; } = new[]
        {
            Intervals.OneMinute, Intervals.ThreeMinutes, Intervals.FiveMinutes,
            Intervals.FifteenMinutes, Intervals.ThirtyMinutes,
            Intervals.OneHour, Intervals.TwoHours, Intervals.FourHours,
            Intervals.SixHours, Intervals.EightHours, Intervals.TwelveHours,
            Intervals.OneDay, Intervals.ThreeDays, Intervals.OneWeek, Intervals.OneMonth
        };

        public BinanceFetcher(HttpClient httpClient, ILogger<BinanceFetcher> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Candle>> FetchCandlesAsync(
            string symbol,
            string interval,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            if (!SupportedIntervals.Contains(interval))
                throw new ArgumentException($"Interval '{interval}' is not supported by Binance.");

            var allCandles = new List<Candle>();

            // Binance expects symbol without separator: BTCUSD not BTC-USD
            var binanceSymbol = NormalizeToBinanceSymbol(symbol);

            var startMs = ToUnixMs(from);
            var endMs = ToUnixMs(to);

            _logger.LogInformation(
                "Fetching {Symbol} {Interval} from Binance: {From:yyyy-MM-dd} to {To:yyyy-MM-dd}",
                symbol, interval, from, to);

            while (startMs < endMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var url = $"{BaseUrl}/api/v3/klines" +
                    $"?symbol={binanceSymbol}" +
                    $"&interval={interval}" +
                    $"&startTime={startMs}" +
                    $"&endTime={endMs}" +
                    $"&limit={MaxCandlesPerRequest}";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var klines = JsonSerializer.Deserialize<JsonElement[]>(json);

                if (klines == null || klines.Length == 0)
                    break;

                foreach (var k in klines)
                {
                    // Binance kline format is a JSON array:
                    // [0] Open time (ms), [1] Open, [2] High, [3] Low, [4] Close,
                    // [5] Volume, [6] Close time, [7] Quote asset volume,
                    // [8] Number of trades, [9] Taker buy base vol,
                    // [10] Taker buy quote vol, [11] Ignore

                    var candle = new Candle
                    {
                        Symbol = symbol, // Keep the normalized symbol (e.g., "BTC-USD"), not Binance's format
                        Source = SourceName,
                        Interval = interval,
                        Timestamp = FromUnixMs(k[0].GetInt64()),
                        Open = double.Parse(k[1].GetString()!),
                        High = double.Parse(k[2].GetString()!),
                        Low = double.Parse(k[3].GetString()!),
                        Close = double.Parse(k[4].GetString()!),
                        Volume = double.Parse(k[5].GetString()!),
                        TradeCount = k[8].GetInt64()
                    };

                    allCandles.Add(candle);
                }

                _logger.LogDebug("Fetched {Count} candles, total so far: {Total}",
                    klines.Length, allCandles.Count);

                // If we got fewer than the max, we've reached the end
                if (klines.Length < MaxCandlesPerRequest)
                    break;

                // Move start past the last candle we received
                startMs = klines[^1][0].GetInt64() + 1;

                // Brief pause between pages
                await Task.Delay(PageDelayMs, cancellationToken);
            }

            _logger.LogInformation("Fetched {Total} total candles from Binance for {Symbol} {Interval}",
                allCandles.Count, symbol, interval);

            return allCandles;
        }

        // ============================================
        // HELPERS
        // ============================================

        /// <summary>
        /// Converts symbols like "BTC-USD" or "BTC/USD" to Binance format "BTCUSD".
        /// Pass-through if already in Binance format.
        /// </summary>
        private static string NormalizeToBinanceSymbol(string symbol)
        {
            return symbol.Replace("-", "").Replace("/", "").ToUpperInvariant();
        }

        private static long ToUnixMs(DateTime dt)
        {
            return new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        private static DateTime FromUnixMs(long ms)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }
    }
}
