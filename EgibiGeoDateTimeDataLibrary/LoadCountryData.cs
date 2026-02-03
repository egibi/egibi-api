#nullable disable
using CsvHelper;
using CsvHelper.Configuration;
using EgibiGeoDateTimeDataLibrary.Models;
using System.Globalization;
using System.Reflection;

namespace EgibiGeoDateTimeDataLibrary
{
    public class LoadCountryData
    {
        public List<CountryData> Load()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("EgibiGeoDateTimeDataLibrary.Files.country.csv"))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Embedded resource not found.");
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false
                    };

                    using (CsvReader csv = new CsvReader(reader, config))
                    {
                        return csv.GetRecords<CountryData>().ToList();
                    }
                }
            }
        }
    }


}
