using egibi_api.Configuration;
using egibi_api.MarketData.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using QuestDB;

namespace egibi_api.MarketData.Repositories
{
    /// <summary>
    /// QuestDB-backed OHLC repository.
    /// Reads via PG wire protocol (Npgsql on port 8812).
    /// Writes via ILP (InfluxDB Line Protocol on port 9009).
    /// </summary>
    public class OhlcRepository : IOhlcRepository
    {
        private readonly string _pgConnectionString;
        private readonly QuestDbOptions _options;
        private readonly ILogger<OhlcRepository> _logger;

        public OhlcRepository(
            IConfiguration configuration,
            IOptions<QuestDbOptions> options,
            ILogger<OhlcRepository> logger)
        {
            _pgConnectionString = configuration.GetConnectionString("QuestDb")
                ?? throw new ArgumentNullException("ConnectionStrings:QuestDb is not configured.");
            _options = options.Value;
            _logger = logger;
        }

        // ============================================
        // COVERAGE QUERIES
        // ============================================

        public async Task<CoverageInfo> GetCoverageAsync(string symbol, string source, string interval)
        {
            var coverage = new CoverageInfo
            {
                Symbol = symbol,
                Source = source,
                Interval = interval
            };

            await using var conn = new NpgsqlConnection(_pgConnectionString);
            await conn.OpenAsync();

            // QuestDB supports standard SQL aggregates over the designated timestamp
            var sql = @"
                SELECT min(timestamp) as earliest,
                       max(timestamp) as latest,
                       count()        as candle_count
                FROM ohlc
                WHERE symbol  = $1
                  AND source  = $2
                  AND interval = $3";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(symbol);
            cmd.Parameters.AddWithValue(source);
            cmd.Parameters.AddWithValue(interval);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    coverage.Earliest = reader.GetDateTime(0);
                    coverage.Latest = reader.GetDateTime(1);
                    coverage.CandleCount = reader.GetInt64(2);
                }
            }

            return coverage;
        }

        public async Task<List<SourceSummary>> GetSourceSummariesAsync(string symbol)
        {
            var summaries = new List<SourceSummary>();

            await using var conn = new NpgsqlConnection(_pgConnectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT source, interval,
                       min(timestamp) as earliest,
                       max(timestamp) as latest,
                       count()        as candle_count
                FROM ohlc
                WHERE symbol = $1
                GROUP BY source, interval
                ORDER BY source, interval";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(symbol);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summaries.Add(new SourceSummary
                {
                    Source = reader.GetString(0),
                    Interval = reader.GetString(1),
                    Earliest = reader.GetDateTime(2),
                    Latest = reader.GetDateTime(3),
                    CandleCount = reader.GetInt64(4)
                });
            }

            return summaries;
        }

        public async Task<List<string>> GetAvailableSymbolsAsync()
        {
            var symbols = new List<string>();

            await using var conn = new NpgsqlConnection(_pgConnectionString);
            await conn.OpenAsync();

            var sql = "SELECT DISTINCT symbol FROM ohlc ORDER BY symbol";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                symbols.Add(reader.GetString(0));
            }

            return symbols;
        }

        // ============================================
        // READ
        // ============================================

        public async Task<List<Candle>> GetCandlesAsync(
            string symbol, string source, string interval,
            DateTime from, DateTime to)
        {
            var candles = new List<Candle>();

            await using var conn = new NpgsqlConnection(_pgConnectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT timestamp, open, high, low, close, volume, trade_count
                FROM ohlc
                WHERE symbol   = $1
                  AND source   = $2
                  AND interval = $3
                  AND timestamp >= $4
                  AND timestamp <= $5
                ORDER BY timestamp ASC";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(symbol);
            cmd.Parameters.AddWithValue(source);
            cmd.Parameters.AddWithValue(interval);
            cmd.Parameters.AddWithValue(from.ToUniversalTime());
            cmd.Parameters.AddWithValue(to.ToUniversalTime());

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                candles.Add(new Candle
                {
                    Symbol = symbol,
                    Source = source,
                    Interval = interval,
                    Timestamp = reader.GetDateTime(0),
                    Open = reader.GetDouble(1),
                    High = reader.GetDouble(2),
                    Low = reader.GetDouble(3),
                    Close = reader.GetDouble(4),
                    Volume = reader.GetDouble(5),
                    TradeCount = reader.IsDBNull(6) ? 0 : reader.GetInt64(6)
                });
            }

            return candles;
        }

        // ============================================
        // WRITE
        // ============================================

        public async Task<int> WriteCandlesAsync(IEnumerable<Candle> candles)
        {
            var count = 0;
            var ilpConnectionString = $"http::addr={_options.IlpHost}:{_options.IlpPort};";

            using var sender = Sender.New(ilpConnectionString);

            foreach (var c in candles)
            {
                await sender.Table("ohlc")
                    .Symbol("symbol", c.Symbol)
                    .Symbol("source", c.Source)
                    .Symbol("interval", c.Interval)
                    .Column("open", c.Open)
                    .Column("high", c.High)
                    .Column("low", c.Low)
                    .Column("close", c.Close)
                    .Column("volume", c.Volume)
                    .Column("trade_count", c.TradeCount)
                    .AtAsync(c.Timestamp.ToUniversalTime());

                count++;
            }

            await sender.SendAsync();

            _logger.LogInformation(
                "Wrote {Count} candles to QuestDB (symbol={Symbol}, source={Source}, interval={Interval})",
                count,
                candles.FirstOrDefault()?.Symbol ?? "?",
                candles.FirstOrDefault()?.Source ?? "?",
                candles.FirstOrDefault()?.Interval ?? "?");

            return count;
        }

        // ============================================
        // ADMIN
        // ============================================

        public async Task EnsureTableExistsAsync()
        {
            await using var conn = new NpgsqlConnection(_pgConnectionString);
            await conn.OpenAsync();

            // Check if table exists
            var checkSql = "SELECT count() FROM tables() WHERE table_name = 'ohlc'";
            await using var checkCmd = new NpgsqlCommand(checkSql, conn);
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0L) > 0;

            if (exists)
            {
                _logger.LogInformation("QuestDB ohlc table already exists.");
                return;
            }

            // Create the table
            var createSql = @"
                CREATE TABLE IF NOT EXISTS ohlc (
                    symbol       SYMBOL,
                    source       SYMBOL,
                    interval     SYMBOL,
                    open         DOUBLE,
                    high         DOUBLE,
                    low          DOUBLE,
                    close        DOUBLE,
                    volume       DOUBLE,
                    trade_count  LONG,
                    timestamp    TIMESTAMP
                ) TIMESTAMP(timestamp) PARTITION BY MONTH
                  WAL
                  DEDUP UPSERT KEYS(symbol, source, interval, timestamp)";

            await using var createCmd = new NpgsqlCommand(createSql, conn);
            await createCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Created QuestDB ohlc table with DEDUP upsert.");
        }

        public async Task<List<string>> ListTablesAsync()
        {
            var tables = new List<string>();

            await using var conn = new NpgsqlConnection(_pgConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT name FROM tables();", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
    }
}