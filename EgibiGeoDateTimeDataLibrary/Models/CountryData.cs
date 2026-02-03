#nullable disable
using CsvHelper.Configuration.Attributes;

namespace EgibiGeoDateTimeDataLibrary.Models
{
    public class CountryData
    {
        [Index(0)]
        public string CountryCode { get; set; }
        [Index(1)]
        public string CountryName { get; set; }

    }
}
