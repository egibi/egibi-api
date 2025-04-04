#nullable disable
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities
{    
    public class Connection
    {
        public int ConnectionID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecretKey { get; set; }
        public bool? IsDataSource { get; set; }

        public int? ConnectionTypeID { get; set; }
        public virtual ConnectionType ConnectionType { get; set; }
    }
}
