namespace egibi_api.MarketData.Models
{
    /// <summary>
    /// A single OHLCV candle with source provenance.
    /// This is the universal format — exchange fetchers, CSV importers,
    /// and QuestDB queries all produce and consume this type.
    /// </summary>
    public class Candle
    {
        public string Symbol { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public long TradeCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Describes what data already exists in QuestDB for a specific
    /// (symbol, source, interval) combination.
    /// </summary>
    public class CoverageInfo
    {
        public string Symbol { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;

        /// <summary>Earliest candle timestamp in the database.</summary>
        public DateTime? Earliest { get; set; }

        /// <summary>Latest candle timestamp in the database.</summary>
        public DateTime? Latest { get; set; }

        /// <summary>Total candle count stored.</summary>
        public long CandleCount { get; set; }

        /// <summary>True if no data exists for this combination.</summary>
        public bool IsEmpty => CandleCount == 0;

        /// <summary>
        /// Check if the stored data fully covers a requested range.
        /// </summary>
        public bool FullyCovers(DateTime from, DateTime to)
        {
            if (IsEmpty) return false;
            return Earliest.HasValue && Latest.HasValue
                && Earliest.Value <= from && Latest.Value >= to;
        }
    }

    /// <summary>
    /// A request for market data. Used by the DataService orchestrator
    /// and passed to exchange fetchers when gaps need filling.
    /// </summary>
    public class MarketDataRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    /// <summary>
    /// The result of a market data request, including what was served
    /// from cache vs. what was fetched fresh.
    /// </summary>
    public class MarketDataResult
    {
        public string Symbol { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<Candle> Candles { get; set; } = new();

        /// <summary>Number of candles that were already in the database.</summary>
        public int CachedCount { get; set; }

        /// <summary>Number of candles freshly fetched and stored.</summary>
        public int FetchedCount { get; set; }
    }

    /// <summary>
    /// Summary of a data source's availability for a given symbol.
    /// Used to populate source/interval dropdowns in the UI.
    /// </summary>
    public class SourceSummary
    {
        public string Source { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public DateTime Earliest { get; set; }
        public DateTime Latest { get; set; }
        public long CandleCount { get; set; }
    }

    /// <summary>
    /// Time range representing a gap in stored data that needs fetching.
    /// </summary>
    public class DataGap
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    /// <summary>
    /// Standard interval values. Matches Binance kline intervals
    /// and serves as the canonical set across all sources.
    /// </summary>
    public static class Intervals
    {
        public const string OneMinute = "1m";
        public const string ThreeMinutes = "3m";
        public const string FiveMinutes = "5m";
        public const string FifteenMinutes = "15m";
        public const string ThirtyMinutes = "30m";
        public const string OneHour = "1h";
        public const string TwoHours = "2h";
        public const string FourHours = "4h";
        public const string SixHours = "6h";
        public const string EightHours = "8h";
        public const string TwelveHours = "12h";
        public const string OneDay = "1d";
        public const string ThreeDays = "3d";
        public const string OneWeek = "1w";
        public const string OneMonth = "1M";

        /// <summary>
        /// Returns the expected duration of one candle for gap calculations.
        /// </summary>
        public static TimeSpan? GetDuration(string interval) => interval switch
        {
            OneMinute => TimeSpan.FromMinutes(1),
            ThreeMinutes => TimeSpan.FromMinutes(3),
            FiveMinutes => TimeSpan.FromMinutes(5),
            FifteenMinutes => TimeSpan.FromMinutes(15),
            ThirtyMinutes => TimeSpan.FromMinutes(30),
            OneHour => TimeSpan.FromHours(1),
            TwoHours => TimeSpan.FromHours(2),
            FourHours => TimeSpan.FromHours(4),
            SixHours => TimeSpan.FromHours(6),
            EightHours => TimeSpan.FromHours(8),
            TwelveHours => TimeSpan.FromHours(12),
            OneDay => TimeSpan.FromDays(1),
            ThreeDays => TimeSpan.FromDays(3),
            OneWeek => TimeSpan.FromDays(7),
            _ => null
        };
    }
}
