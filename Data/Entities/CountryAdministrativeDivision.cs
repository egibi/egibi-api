namespace egibi_api.Data.Entities
#nullable disable
{
    public class CountryAdministrativeDivision : EntityBase
    {
        public string Abbreviation { get; set; }
        public bool ObservesDst { get; set; }
        public CountryAdministrativeDivisionType CountryAdministrativeDivisionType;
        public ICollection<TimeZone> TimeZones;
    }
}
