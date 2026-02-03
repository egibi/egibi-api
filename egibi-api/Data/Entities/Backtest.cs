#nullable disable
namespace egibi_api.Data.Entities
{
    public class Backtest : EntityBase
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public int? ConnectionId { get; set; }
        public virtual Connection Connection { get; set; }

        public int? DataProviderId { get; set; }
        public virtual DataProvider DataProvider { get; set; }

        public int? StrategyId { get; set; }
        public virtual Strategy Strategy { get; set; }

    }
}
