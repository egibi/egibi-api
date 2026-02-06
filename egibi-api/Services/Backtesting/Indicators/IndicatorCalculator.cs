namespace egibi_api.Services.Backtesting.Indicators;

/// <summary>
/// Pure, stateless indicator calculations operating on arrays of closing prices.
/// All methods return arrays aligned with the input (leading values are NaN where
/// there isn't enough data to compute the indicator).
/// </summary>
public static class IndicatorCalculator
{
    // ═══════════════════════════════════════════════════════
    //  SIMPLE MOVING AVERAGE
    // ═══════════════════════════════════════════════════════

    public static double[] SMA(double[] closes, int period)
    {
        var result = new double[closes.Length];
        Array.Fill(result, double.NaN);

        if (closes.Length < period) return result;

        double sum = 0;
        for (int i = 0; i < period; i++)
            sum += closes[i];

        result[period - 1] = sum / period;

        for (int i = period; i < closes.Length; i++)
        {
            sum += closes[i] - closes[i - period];
            result[i] = sum / period;
        }

        return result;
    }


    // ═══════════════════════════════════════════════════════
    //  EXPONENTIAL MOVING AVERAGE
    // ═══════════════════════════════════════════════════════

    public static double[] EMA(double[] closes, int period)
    {
        var result = new double[closes.Length];
        Array.Fill(result, double.NaN);

        if (closes.Length < period) return result;

        // Seed with SMA
        double sum = 0;
        for (int i = 0; i < period; i++)
            sum += closes[i];

        double multiplier = 2.0 / (period + 1);
        result[period - 1] = sum / period;

        for (int i = period; i < closes.Length; i++)
        {
            result[i] = (closes[i] - result[i - 1]) * multiplier + result[i - 1];
        }

        return result;
    }


    // ═══════════════════════════════════════════════════════
    //  RELATIVE STRENGTH INDEX
    // ═══════════════════════════════════════════════════════

    public static double[] RSI(double[] closes, int period = 14)
    {
        var result = new double[closes.Length];
        Array.Fill(result, double.NaN);

        if (closes.Length < period + 1) return result;

        // Calculate price changes
        var gains = new double[closes.Length];
        var losses = new double[closes.Length];

        for (int i = 1; i < closes.Length; i++)
        {
            double change = closes[i] - closes[i - 1];
            gains[i] = change > 0 ? change : 0;
            losses[i] = change < 0 ? -change : 0;
        }

        // First average: simple average of first `period` values
        double avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            avgGain += gains[i];
            avgLoss += losses[i];
        }
        avgGain /= period;
        avgLoss /= period;

        result[period] = avgLoss == 0 ? 100 : 100 - (100 / (1 + avgGain / avgLoss));

        // Subsequent values: smoothed (Wilder's) average
        for (int i = period + 1; i < closes.Length; i++)
        {
            avgGain = (avgGain * (period - 1) + gains[i]) / period;
            avgLoss = (avgLoss * (period - 1) + losses[i]) / period;

            result[i] = avgLoss == 0 ? 100 : 100 - (100 / (1 + avgGain / avgLoss));
        }

        return result;
    }


    // ═══════════════════════════════════════════════════════
    //  MACD (returns MACD line, Signal line, Histogram)
    // ═══════════════════════════════════════════════════════

    public static (double[] MacdLine, double[] SignalLine, double[] Histogram) MACD(
        double[] closes, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var macdLine = new double[closes.Length];
        var signalLine = new double[closes.Length];
        var histogram = new double[closes.Length];
        Array.Fill(macdLine, double.NaN);
        Array.Fill(signalLine, double.NaN);
        Array.Fill(histogram, double.NaN);

        var fastEma = EMA(closes, fastPeriod);
        var slowEma = EMA(closes, slowPeriod);

        // MACD line = fast EMA - slow EMA
        int macdStart = -1;
        for (int i = 0; i < closes.Length; i++)
        {
            if (!double.IsNaN(fastEma[i]) && !double.IsNaN(slowEma[i]))
            {
                macdLine[i] = fastEma[i] - slowEma[i];
                if (macdStart < 0) macdStart = i;
            }
        }

        if (macdStart < 0) return (macdLine, signalLine, histogram);

        // Signal line = EMA of MACD line
        // Extract valid MACD values for EMA calculation
        var validMacd = macdLine.Skip(macdStart).Where(v => !double.IsNaN(v)).ToArray();
        if (validMacd.Length < signalPeriod)
            return (macdLine, signalLine, histogram);

        var signalEma = EMA(validMacd, signalPeriod);

        // Map signal back to original indices
        int j = 0;
        for (int i = macdStart; i < closes.Length; i++)
        {
            if (!double.IsNaN(macdLine[i]) && j < signalEma.Length)
            {
                signalLine[i] = signalEma[j];
                if (!double.IsNaN(signalEma[j]))
                    histogram[i] = macdLine[i] - signalEma[j];
                j++;
            }
        }

        return (macdLine, signalLine, histogram);
    }


    // ═══════════════════════════════════════════════════════
    //  BOLLINGER BANDS (returns Upper, Middle, Lower)
    // ═══════════════════════════════════════════════════════

    public static (double[] Upper, double[] Middle, double[] Lower) BollingerBands(
        double[] closes, int period = 20, double stdDevMultiplier = 2.0)
    {
        var upper = new double[closes.Length];
        var middle = SMA(closes, period);
        var lower = new double[closes.Length];
        Array.Fill(upper, double.NaN);
        Array.Fill(lower, double.NaN);

        for (int i = period - 1; i < closes.Length; i++)
        {
            if (double.IsNaN(middle[i])) continue;

            double sumSq = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                double diff = closes[j] - middle[i];
                sumSq += diff * diff;
            }
            double stdDev = Math.Sqrt(sumSq / period);

            upper[i] = middle[i] + stdDevMultiplier * stdDev;
            lower[i] = middle[i] - stdDevMultiplier * stdDev;
        }

        return (upper, middle, lower);
    }
}
