using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities;

/// <summary>
/// Trading strategy definition.
/// 
/// Two modes:
///   1. Simple (UI-created): RulesConfiguration contains JSON with indicator-based 
///      entry/exit conditions. Executed by SimpleBacktestEngine.
///   2. Code-only: StrategyClassName references a C# class implementing IStrategy.
///      RulesConfiguration is null.
/// </summary>
public class Strategy
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON blob storing the StrategyConfigurationDto for UI-created strategies.
    /// Null for code-only strategies.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? RulesConfiguration { get; set; }

    /// <summary>
    /// Fully qualified class name for code-only strategies.
    /// e.g., "egibi_api.Strategies.MomentumStrategy"
    /// Null for UI-created strategies.
    /// </summary>
    [MaxLength(500)]
    public string? StrategyClassName { get; set; }

    /// <summary>
    /// Whether this is a simple (UI-created) or coded strategy.
    /// </summary>
    public bool IsSimple { get; set; } = true;

    /// <summary>
    /// The exchange account this strategy targets for live trading.
    /// </summary>
    public int? ExchangeAccountId { get; set; }

    [ForeignKey(nameof(ExchangeAccountId))]
    public virtual ExchangeAccount? ExchangeAccount { get; set; }

    /// <summary>
    /// Is this strategy currently active for live trading?
    /// </summary>
    public bool IsActive { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<Backtest> Backtests { get; set; } = new List<Backtest>();
}
