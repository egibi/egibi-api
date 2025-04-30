#nullable disable
namespace egibi_api.Data.Entities
{
    public class Backtest
    {
        public int BacktestID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public int? ConnectionId { get; set; }
        public virtual Connection Connection { get; set; }

        public int? StrategyId { get; set; }
        public virtual Strategy Strategy { get; set; }

    }
}
