-- ============================================
-- Egibi QuestDB — OHLC Candle Schema
-- ============================================
-- Run this in QuestDB web console (http://localhost:9000)
-- OR let the API auto-create it via EnsureTableExistsAsync()
-- ============================================

CREATE TABLE IF NOT EXISTS ohlc (
    symbol       SYMBOL,        -- BTC-USD, ETH-USD
    source       SYMBOL,        -- binance, coinbase, csv:filename, alphavantage
    interval     SYMBOL,        -- 1m, 5m, 1h, 1d, 1w
    open         DOUBLE,
    high         DOUBLE,
    low          DOUBLE,
    close        DOUBLE,
    volume       DOUBLE,
    trade_count  LONG,
    timestamp    TIMESTAMP
) TIMESTAMP(timestamp) PARTITION BY MONTH
  WAL
  DEDUP UPSERT KEYS(symbol, source, interval, timestamp);
