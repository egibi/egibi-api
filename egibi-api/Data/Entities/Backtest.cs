using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities;

/// <summary>
/// Records a backtest execution and its results.
/// The full result detail (equity curve, trade log) is stored in ResultJson.
/// Summary stats are denormalized into columns for easy querying/display in grids.
/// </summary>
public class Backtest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    // ── Strategy Link ──────────────────────────────────────
    public int StrategyId { get; set; }

    [ForeignKey(nameof(StrategyId))]
    public virtual Strategy Strategy { get; set; } = null!;

    // ── Status ─────────────────────────────────────────────
    public int BacktestStatusId { get; set; }

    [ForeignKey(nameof(BacktestStatusId))]
    public virtual BacktestStatus BacktestStatus { get; set; } = null!;

    // ── Configuration ──────────────────────────────────────
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }

    // ── Summary Results (denormalized for grid display) ────
    public decimal? FinalCapital { get; set; }
    public decimal? TotalReturnPct { get; set; }
    public int? TotalTrades { get; set; }
    public decimal? WinRate { get; set; }
    public decimal? MaxDrawdownPct { get; set; }
    public decimal? SharpeRatio { get; set; }

    // ── Full Result (JSON blob) ────────────────────────────
    /// <summary>
    /// Complete BacktestResultDto serialized as JSON.
    /// Contains equity curve, trade log, and all computed stats.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ResultJson { get; set; }

    // ── Timestamps ─────────────────────────────────────────
    public DateTime? ExecutedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
