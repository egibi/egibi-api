#nullable disable
namespace egibi_api.Data.QuestEntities
{
    public class BinanceUsPriceData
    {
        public DateTime Timestamp { get; set; }
        public string Symbol { get; set; }
        public double Price { get; set; }
    }
}
