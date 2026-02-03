#nullable disable
using CsvHelper.Configuration.Attributes;

namespace EgibiGeoDateTimeDataLibrary.Models
{
    public class TimeZoneData
    {
        [Index(0)]
        public string ZoneName { get; set; }
        [Index(1)]
        public string CountryCode { get; set; }
        [Index(2)]
        public string Abbreviation { get; set; }
        [Index(3)]
        public string TimeStart { get; set; }
        [Index(4)]
        public string GmtOffset { get; set; }
        [Index(5)]
        public string Dst { get; set; }
    }
}
