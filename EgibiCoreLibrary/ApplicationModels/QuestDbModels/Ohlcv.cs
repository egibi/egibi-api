#nullable disable
namespace EgibiCoreLibrary.Models.QuestDbModels
{
    public class Ohlcv
    {
       public DateTime TimeStamp { get; set; }
       public decimal Open { get; set; }
       public decimal High { get; set; }
       public decimal Low { get; set; }
       public decimal Close { get; set; }
       public decimal Volume { get; set; }
       public DateTime DateTime { get; set; }
    }
}
