using System.Diagnostics;
using egibi_api.Models.Strategy;
using egibi_api.Services.Backtesting.Indicators;

namespace egibi_api.Services.Backtesting;

/// <summary>
/// Executes a simple rule-based strategy against an array of OHLC candles.
/// This handles UI-created strategies only. Hand-coded IStrategy implementations
/// will use a different execution path.
/// </summary>
public class SimpleBacktestEngine
{
    /// <summary>
    /// Run a backtest with the given configuration against the provided candle data.
    /// </summary>
    public BacktestResultDto Execute(
        StrategyConfigurationDto config,
        List<OhlcCandle> candles,
        decimal initialCapital,
        DateTime startDate,
        DateTime endDate)
    {
        var sw = Stopwatch.StartNew();
        var result = new BacktestResultDto
        {
            InitialCapital = initialCapital,
            Symbol = config.Symbol,
            DataSource = config.DataSource,
            Interval = config.Interval,
            StartDate = startDate,
            EndDate = endDate,
            CandlesProcessed = candles.Count
        };

        if (candles.Count < 2)
        {
            result.Warnings.Add("Insufficient data for backtesting (need at least 2 candles).");
            result.FinalCapital = initialCapital;
            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        // ── Pre-compute all indicators ─────────────────────
        var closes = candles.Select(c => (double)c.Close).ToArray();
        var indicatorCache = PrecomputeIndicators(config, closes);

        // ── Simulation State ───────────────────────────────
        decimal equity = initialCapital;
        decimal peakEquity = initialCapital;
        decimal maxDrawdown = 0;
        bool inPosition = false;
        BacktestTradeDto? openTrade = null;
        int tradeCount = 0;
        var trades = new List<BacktestTradeDto>();
        var equityCurve = new List<EquityPointDto>();

        // First equity point
        equityCurve.Add(new EquityPointDto
        {
            Timestamp = candles[0].Timestamp,
            Equity = equity,
            DrawdownPct = 0
        });

        // ── Walk through candles ───────────────────────────
        // Start at index where all indicators have values
        int startIndex = GetMinimumWarmupIndex(config, indicatorCache);

        for (int i = startIndex; i < candles.Count; i++)
        {
            var candle = candles[i];
            decimal currentPrice = candle.Close;

            if (inPosition && openTrade != null)
            {
                // ── Check stop loss / take profit ──────────
                if (config.StopLossPct.HasValue)
                {
                    decimal stopPrice = openTrade.Side == "LONG"
                        ? openTrade.EntryPrice * (1 - config.StopLossPct.Value / 100)
                        : openTrade.EntryPrice * (1 + config.StopLossPct.Value / 100);

                    bool stopHit = openTrade.Side == "LONG"
                        ? candle.Low <= stopPrice
                        : candle.High >= stopPrice;

                    if (stopHit)
                    {
                        CloseTrade(openTrade, stopPrice, candle.Timestamp, "STOP_LOSS", ref equity);
                        trades.Add(openTrade);
                        openTrade = null;
                        inPosition = false;
                    }
                }

                if (inPosition && config.TakeProfitPct.HasValue && openTrade != null)
                {
                    decimal tpPrice = openTrade.Side == "LONG"
                        ? openTrade.EntryPrice * (1 + config.TakeProfitPct.Value / 100)
                        : openTrade.EntryPrice * (1 - config.TakeProfitPct.Value / 100);

                    bool tpHit = openTrade.Side == "LONG"
                        ? candle.High >= tpPrice
                        : candle.Low <= tpPrice;

                    if (tpHit)
                    {
                        CloseTrade(openTrade, tpPrice, candle.Timestamp, "TAKE_PROFIT", ref equity);
                        trades.Add(openTrade);
                        openTrade = null;
                        inPosition = false;
                    }
                }

                // ── Check exit signal ──────────────────────
                if (inPosition && openTrade != null &&
                    EvaluateConditions(config.ExitConditions, config.ExitLogic, indicatorCache, closes, i))
                {
                    CloseTrade(openTrade, currentPrice, candle.Timestamp, "SIGNAL", ref equity);
                    trades.Add(openTrade);
                    openTrade = null;
                    inPosition = false;
                }
            }
            else
            {
                // ── Check entry signal ─────────────────────
                if (EvaluateConditions(config.EntryConditions, config.EntryLogic, indicatorCache, closes, i))
                {
                    tradeCount++;
                    decimal positionSize = equity * (config.PositionSizePct / 100);
                    decimal quantity = positionSize / currentPrice;

                    openTrade = new BacktestTradeDto
                    {
                        TradeNumber = tradeCount,
                        Side = "LONG", // TODO: SHORT when AllowShort is true and conditions dictate
                        EntryTime = candle.Timestamp,
                        EntryPrice = currentPrice,
                        Quantity = quantity
                    };
                    inPosition = true;
                }
            }

            // ── Update equity (mark-to-market if in position) ──
            decimal currentEquity = equity;
            if (inPosition && openTrade != null)
            {
                decimal unrealizedPnL = openTrade.Side == "LONG"
                    ? (currentPrice - openTrade.EntryPrice) * openTrade.Quantity
                    : (openTrade.EntryPrice - currentPrice) * openTrade.Quantity;
                currentEquity = equity + unrealizedPnL;
            }

            if (currentEquity > peakEquity) peakEquity = currentEquity;
            decimal drawdown = peakEquity > 0 ? (peakEquity - currentEquity) / peakEquity * 100 : 0;
            if (drawdown > maxDrawdown) maxDrawdown = drawdown;

            equityCurve.Add(new EquityPointDto
            {
                Timestamp = candle.Timestamp,
                Equity = Math.Round(currentEquity, 2),
                DrawdownPct = Math.Round(drawdown, 2)
            });
        }

        // ── Close any open trade at the end ────────────────
        if (inPosition && openTrade != null)
        {
            var lastCandle = candles[^1];
            CloseTrade(openTrade, lastCandle.Close, lastCandle.Timestamp, "END_OF_DATA", ref equity);
            trades.Add(openTrade);
            result.Warnings.Add("Open position was closed at end of data period.");
        }

        // ── Compute summary stats ──────────────────────────
        result.FinalCapital = Math.Round(equity, 2);
        result.TotalReturnPct = initialCapital > 0
            ? Math.Round((equity - initialCapital) / initialCapital * 100, 2)
            : 0;
        result.MaxDrawdownPct = Math.Round(maxDrawdown, 2);
        result.TotalTrades = trades.Count;
        result.WinningTrades = trades.Count(t => t.PnL > 0);
        result.LosingTrades = trades.Count(t => t.PnL <= 0);
        result.WinRate = trades.Count > 0
            ? Math.Round((decimal)result.WinningTrades / trades.Count * 100, 2)
            : 0;

        var wins = trades.Where(t => t.PnL > 0).ToList();
        var losses = trades.Where(t => t.PnL <= 0).ToList();

        decimal totalWins = wins.Sum(t => t.PnL);
        decimal totalLosses = Math.Abs(losses.Sum(t => t.PnL));
        result.ProfitFactor = totalLosses > 0 ? Math.Round(totalWins / totalLosses, 2) : totalWins > 0 ? 999.99m : 0;

        result.AverageWinPct = wins.Count > 0 ? Math.Round(wins.Average(t => t.PnLPct), 2) : 0;
        result.AverageLossPct = losses.Count > 0 ? Math.Round(losses.Average(t => t.PnLPct), 2) : 0;
        result.LargestWinPct = wins.Count > 0 ? Math.Round(wins.Max(t => t.PnLPct), 2) : 0;
        result.LargestLossPct = losses.Count > 0 ? Math.Round(losses.Min(t => t.PnLPct), 2) : 0;

        if (trades.Count > 0)
        {
            result.AverageHoldTime = TimeSpan.FromTicks(
                (long)trades.Average(t => t.HoldDuration.Ticks));
        }

        // Sharpe Ratio (annualized, assuming daily returns)
        if (equityCurve.Count > 1)
        {
            var returns = new List<double>();
            for (int i = 1; i < equityCurve.Count; i++)
            {
                if (equityCurve[i - 1].Equity > 0)
                    returns.Add((double)((equityCurve[i].Equity - equityCurve[i - 1].Equity) / equityCurve[i - 1].Equity));
            }

            if (returns.Count > 1)
            {
                double avgReturn = returns.Average();
                double stdDev = Math.Sqrt(returns.Average(r => Math.Pow(r - avgReturn, 2)));
                double annualizationFactor = Math.Sqrt(AnnualizationMultiplier(config.Interval));
                result.SharpeRatio = stdDev > 0
                    ? Math.Round((decimal)(avgReturn / stdDev * annualizationFactor), 2)
                    : 0;
            }
        }

        result.Trades = trades;
        result.EquityCurve = equityCurve;

        sw.Stop();
        result.ExecutionTimeMs = sw.ElapsedMilliseconds;

        return result;
    }


    // ═══════════════════════════════════════════════════════
    //  INDICATOR PRECOMPUTATION
    // ═══════════════════════════════════════════════════════

    private Dictionary<string, double[]> PrecomputeIndicators(StrategyConfigurationDto config, double[] closes)
    {
        var cache = new Dictionary<string, double[]>();
        var allConditions = config.EntryConditions.Concat(config.ExitConditions).ToList();

        foreach (var cond in allConditions)
        {
            ComputeAndCache(cache, cond.Indicator, cond.Period, closes);

            if (cond.CompareType == "INDICATOR" && !string.IsNullOrEmpty(cond.CompareIndicator))
            {
                ComputeAndCache(cache, cond.CompareIndicator, cond.ComparePeriod ?? cond.Period, closes);
            }
        }

        // Always cache PRICE for convenience
        cache["PRICE"] = closes;

        return cache;
    }

    private void ComputeAndCache(Dictionary<string, double[]> cache, string indicator, int period, double[] closes)
    {
        string key = $"{indicator}_{period}";
        if (cache.ContainsKey(key)) return;

        switch (indicator.ToUpperInvariant())
        {
            case "SMA":
                cache[key] = IndicatorCalculator.SMA(closes, period);
                break;
            case "EMA":
                cache[key] = IndicatorCalculator.EMA(closes, period);
                break;
            case "RSI":
                cache[key] = IndicatorCalculator.RSI(closes, period);
                break;
            case "MACD":
                var (macd, signal, hist) = IndicatorCalculator.MACD(closes);
                cache["MACD_LINE"] = macd;
                cache["MACD_SIGNAL"] = signal;
                cache["MACD_HIST"] = hist;
                break;
            case "BBANDS":
                var (upper, middle, lower) = IndicatorCalculator.BollingerBands(closes, period);
                cache[$"BBANDS_UPPER_{period}"] = upper;
                cache[$"BBANDS_MIDDLE_{period}"] = middle;
                cache[$"BBANDS_LOWER_{period}"] = lower;
                break;
            case "PRICE":
                cache["PRICE"] = closes;
                break;
        }
    }


    // ═══════════════════════════════════════════════════════
    //  CONDITION EVALUATION
    // ═══════════════════════════════════════════════════════

    private bool EvaluateConditions(
        List<StrategyConditionDto> conditions,
        string logic,
        Dictionary<string, double[]> indicators,
        double[] closes,
        int index)
    {
        if (conditions.Count == 0) return false;

        var results = conditions.Select(c => EvaluateSingle(c, indicators, closes, index)).ToList();

        return logic.ToUpperInvariant() == "OR"
            ? results.Any(r => r)
            : results.All(r => r);
    }

    private bool EvaluateSingle(
        StrategyConditionDto condition,
        Dictionary<string, double[]> indicators,
        double[] closes,
        int index)
    {
        double leftValue = GetIndicatorValue(indicators, condition.Indicator, condition.Period, index);
        double leftPrev = index > 0
            ? GetIndicatorValue(indicators, condition.Indicator, condition.Period, index - 1)
            : double.NaN;

        if (double.IsNaN(leftValue)) return false;

        double rightValue;
        double rightPrev;

        if (condition.CompareType == "INDICATOR" && !string.IsNullOrEmpty(condition.CompareIndicator))
        {
            int comparePeriod = condition.ComparePeriod ?? condition.Period;
            rightValue = GetIndicatorValue(indicators, condition.CompareIndicator, comparePeriod, index);
            rightPrev = index > 0
                ? GetIndicatorValue(indicators, condition.CompareIndicator, comparePeriod, index - 1)
                : double.NaN;
        }
        else
        {
            rightValue = (double)(condition.CompareValue ?? 0);
            rightPrev = rightValue; // Static value doesn't change
        }

        if (double.IsNaN(rightValue)) return false;

        return condition.Operator.ToUpperInvariant() switch
        {
            "GREATER_THAN" => leftValue > rightValue,
            "LESS_THAN" => leftValue < rightValue,
            "EQUALS" => Math.Abs(leftValue - rightValue) < 0.0001,
            "CROSSES_ABOVE" => !double.IsNaN(leftPrev) && !double.IsNaN(rightPrev)
                               && leftPrev <= rightPrev && leftValue > rightValue,
            "CROSSES_BELOW" => !double.IsNaN(leftPrev) && !double.IsNaN(rightPrev)
                               && leftPrev >= rightPrev && leftValue < rightValue,
            _ => false
        };
    }

    private double GetIndicatorValue(Dictionary<string, double[]> indicators, string indicator, int period, int index)
    {
        string key = indicator.ToUpperInvariant() switch
        {
            "PRICE" => "PRICE",
            "MACD" => "MACD_LINE",
            "MACD_SIGNAL" => "MACD_SIGNAL",
            "MACD_HIST" => "MACD_HIST",
            "BBANDS_UPPER" => $"BBANDS_UPPER_{period}",
            "BBANDS_MIDDLE" => $"BBANDS_MIDDLE_{period}",
            "BBANDS_LOWER" => $"BBANDS_LOWER_{period}",
            _ => $"{indicator.ToUpperInvariant()}_{period}"
        };

        if (!indicators.TryGetValue(key, out var values)) return double.NaN;
        if (index < 0 || index >= values.Length) return double.NaN;
        return values[index];
    }


    // ═══════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════

    private int GetMinimumWarmupIndex(StrategyConfigurationDto config, Dictionary<string, double[]> indicators)
    {
        // Find the first index where all indicators have non-NaN values
        int maxWarmup = 1;

        foreach (var kvp in indicators)
        {
            for (int i = 0; i < kvp.Value.Length; i++)
            {
                if (!double.IsNaN(kvp.Value[i]))
                {
                    maxWarmup = Math.Max(maxWarmup, i);
                    break;
                }
            }
        }

        return maxWarmup;
    }

    private void CloseTrade(BacktestTradeDto trade, decimal exitPrice, DateTime exitTime, string reason, ref decimal equity)
    {
        trade.ExitPrice = exitPrice;
        trade.ExitTime = exitTime;
        trade.ExitReason = reason;
        trade.HoldDuration = exitTime - trade.EntryTime;

        trade.PnL = trade.Side == "LONG"
            ? (exitPrice - trade.EntryPrice) * trade.Quantity
            : (trade.EntryPrice - exitPrice) * trade.Quantity;

        trade.PnLPct = trade.EntryPrice > 0
            ? Math.Round((trade.Side == "LONG"
                ? (exitPrice - trade.EntryPrice) / trade.EntryPrice
                : (trade.EntryPrice - exitPrice) / trade.EntryPrice) * 100, 2)
            : 0;

        equity += trade.PnL;
        trade.EquityAfter = Math.Round(equity, 2);
    }

    private static double AnnualizationMultiplier(string interval) => interval.ToLowerInvariant() switch
    {
        "1m" => 525_600,
        "5m" => 105_120,
        "15m" => 35_040,
        "1h" => 8_760,
        "4h" => 2_190,
        "1d" => 365,
        "1w" => 52,
        _ => 365
    };
}


/// <summary>
/// Lightweight candle record for the backtest engine.
/// Mapped from QuestDB query results.
/// </summary>
public class OhlcCandle
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
