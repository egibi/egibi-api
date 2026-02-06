namespace egibi_api.Models.Strategy;

// ═══════════════════════════════════════════════════════════
//  BACKTEST REQUEST
// ═══════════════════════════════════════════════════════════

public class BacktestRequestDto
{
    public int StrategyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; } = 10_000m;

    // Optional overrides (if null, uses strategy's saved config)
    public string? Symbol { get; set; }
    public string? DataSource { get; set; }
    public string? Interval { get; set; }
}


// ═══════════════════════════════════════════════════════════
//  BACKTEST RESULT
// ═══════════════════════════════════════════════════════════

public class BacktestResultDto
{
    // ── Summary Stats ──────────────────────────────────────
    public decimal InitialCapital { get; set; }
    public decimal FinalCapital { get; set; }
    public decimal TotalReturnPct { get; set; }
    public decimal MaxDrawdownPct { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal AverageWinPct { get; set; }
    public decimal AverageLossPct { get; set; }
    public decimal LargestWinPct { get; set; }
    public decimal LargestLossPct { get; set; }
    public TimeSpan AverageHoldTime { get; set; }

    // ── Data Info ──────────────────────────────────────────
    public string Symbol { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CandlesProcessed { get; set; }

    // ── Equity Curve ───────────────────────────────────────
    public List<EquityPointDto> EquityCurve { get; set; } = new();

    // ── Trade Log ──────────────────────────────────────────
    public List<BacktestTradeDto> Trades { get; set; } = new();

    // ── Execution Info ─────────────────────────────────────
    public long ExecutionTimeMs { get; set; }
    public List<string> Warnings { get; set; } = new();
}


// ═══════════════════════════════════════════════════════════
//  SUPPORTING TYPES
// ═══════════════════════════════════════════════════════════

public class EquityPointDto
{
    public DateTime Timestamp { get; set; }
    public decimal Equity { get; set; }
    public decimal DrawdownPct { get; set; }
}

public class BacktestTradeDto
{
    public int TradeNumber { get; set; }
    public string Side { get; set; } = "LONG";                // LONG | SHORT
    public DateTime EntryTime { get; set; }
    public decimal EntryPrice { get; set; }
    public DateTime ExitTime { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPct { get; set; }
    public decimal EquityAfter { get; set; }
    public string ExitReason { get; set; } = string.Empty;    // SIGNAL, STOP_LOSS, TAKE_PROFIT
    public TimeSpan HoldDuration { get; set; }
}

/// <summary>
/// Returned by the coverage endpoint so the UI knows what data is available.
/// </summary>
public class DataCoverageDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public long CandleCount { get; set; }
}
