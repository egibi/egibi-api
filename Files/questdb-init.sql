-- ============================================
-- QuestDB OHLC Table Initialization
-- ============================================
-- Run this in the QuestDB web console (http://localhost:9000)
-- or via the HTTP API on first setup.
--
-- QuestDB also auto-creates tables on first ILP write,
-- but defining it explicitly ensures correct types
-- and partitioning from the start.
-- ============================================

CREATE TABLE IF NOT EXISTS ohlc (
    symbol       SYMBOL,        -- Trading pair: BTC-USD, ETH-USD, AAPL, etc.
    source       SYMBOL,        -- Data origin: binance, coinbase, csv:my-file, alphavantage
    interval     SYMBOL,        -- Candle size: 1m, 5m, 15m, 30m, 1h, 4h, 1d, 1w
    open         DOUBLE,
    high         DOUBLE,
    low          DOUBLE,
    close        DOUBLE,
    volume       DOUBLE,
    trade_count  LONG,          -- Number of trades in the candle (if available, else 0)
    timestamp    TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY MONTH
  WAL
  DEDUP UPSERT KEYS(symbol, source, interval, timestamp);

-- ============================================
-- Notes:
-- ============================================
-- SYMBOL type:  Indexed string, fast for WHERE filters and GROUP BY.
--
-- PARTITION BY MONTH:  Data is physically split into monthly directories.
--   - Time-range queries only scan relevant partitions.
--   - Old months can be dropped instantly:
--     ALTER TABLE ohlc DROP PARTITION LIST '2024-01';
--
-- WAL (Write-Ahead Log):  Enables concurrent reads during writes.
--   Required for DEDUP.
--
-- DEDUP UPSERT KEYS:  If you write a candle with the same
--   (symbol, source, interval, timestamp) that already exists,
--   it updates the existing row instead of creating a duplicate.
--   This is the "if it exists, skip it" behavior — you can
--   safely re-ingest overlapping ranges without creating duplicates.
-- ============================================
