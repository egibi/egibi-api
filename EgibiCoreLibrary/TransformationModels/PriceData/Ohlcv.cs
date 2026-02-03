#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EgibiCoreLibrary.TransformationModels.PriceData
{
    public class Ohlcv
    {
        public string Symbol { get; set; }
        public string Timestamp { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
        public string DateTime { get; set; }
    }
}
