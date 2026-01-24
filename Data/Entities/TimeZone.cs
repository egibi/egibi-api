#nullable disable
namespace egibi_api.Data.Entities
{
    public class TimeZone : EntityBase
    {
        public string ZoneName { get; set; }
        public string CountryCode { get; set; }
        public string Abbreviation { get; set; }
        public int? TimeStart { get; set; }
        public int? GmtOffset { get; set; }
        public bool? Dst { get; set; }
    }
}
