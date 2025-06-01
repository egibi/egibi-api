#nullable disable

namespace egibi_api.Data.Entities
{    
    public class Connection : EntityBase
    {
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecretKey { get; set; }
        public bool? IsDataSource { get; set; }

        public int? ConnectionTypeId { get; set; }
        public virtual ConnectionType ConnectionType { get; set; }
    }
}
