#nullable disable
namespace egibi_api.Data.Entities
{
    public class Country : EntityBase
    {
        public string Abbreviation { get; set; }
        public ICollection<CountryAdministrativeDivision> CountryAdministrativeDivisions { get; set; }
    }
}
