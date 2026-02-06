namespace egibi_api.Models.Strategy;

/// <summary>
/// The full configuration for a UI-created simple strategy.
/// Serialized as JSON and stored in Strategy.RulesConfiguration.
/// Hand-coded IStrategy implementations bypass this entirely.
/// </summary>
public class StrategyConfigurationDto
{
    // ── Target Market ──────────────────────────────────────
    public int? ExchangeAccountId { get; set; }
    public string Symbol { get; set; } = string.Empty;       // BTC-USD, ETH-USD
    public string Interval { get; set; } = "1h";              // 1m, 5m, 15m, 1h, 4h, 1d

    // ── Data Source ────────────────────────────────────────
    public string DataSource { get; set; } = string.Empty;    // binance, coinbase, csv:filename

    // ── Entry Conditions ───────────────────────────────────
    public List<StrategyConditionDto> EntryConditions { get; set; } = new();
    public string EntryLogic { get; set; } = "AND";           // AND | OR

    // ── Exit Conditions ────────────────────────────────────
    public List<StrategyConditionDto> ExitConditions { get; set; } = new();
    public string ExitLogic { get; set; } = "AND";            // AND | OR

    // ── Position Sizing ────────────────────────────────────
    public decimal PositionSizePct { get; set; } = 100;       // % of available capital per trade
    public bool AllowShort { get; set; } = false;

    // ── Risk Management ────────────────────────────────────
    public decimal? StopLossPct { get; set; }                  // e.g., 2.0 = 2% stop loss
    public decimal? TakeProfitPct { get; set; }                // e.g., 5.0 = 5% take profit
}

/// <summary>
/// A single condition in an entry or exit rule.
/// e.g., "SMA(20) crosses above SMA(50)" or "RSI(14) &lt; 30"
/// </summary>
public class StrategyConditionDto
{
    // ── Left side (the indicator being evaluated) ──────────
    public string Indicator { get; set; } = string.Empty;     // SMA, EMA, RSI, MACD, BBANDS, PRICE
    public int Period { get; set; }                            // e.g., 14 for RSI(14), 20 for SMA(20)

    // ── Operator ───────────────────────────────────────────
    public string Operator { get; set; } = string.Empty;      // CROSSES_ABOVE, CROSSES_BELOW, GREATER_THAN, LESS_THAN, EQUALS

    // ── Right side (what we're comparing against) ──────────
    public string CompareType { get; set; } = "VALUE";        // VALUE | INDICATOR

    // If CompareType == VALUE:
    public decimal? CompareValue { get; set; }                 // e.g., 30 for "RSI < 30"

    // If CompareType == INDICATOR:
    public string? CompareIndicator { get; set; }              // e.g., SMA, EMA
    public int? ComparePeriod { get; set; }                    // e.g., 50 for SMA(50)
}
