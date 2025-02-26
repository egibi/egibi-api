#nullable disable
namespace egibi_api.Data.Entities
{
    public class ConnectionType
    {
        public int ConnectionTypeID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        //public ICollection<Connection> Connections { get; set; }
    }
}
