using egibi_api.Data.Entities;

namespace egibi_api.Data
{
    public class DbSetup
    {
        public static List<ConnectionType> GetConnectionTypes()
        {
            List<ConnectionType> connectionTypes = new List<ConnectionType>
            {
                new ConnectionType
                {
                    ConnectionTypeID = 1,
                    Name = "unknown",
                    Description = "unknown connection type"
                },
                new ConnectionType
                {
                    ConnectionTypeID = 2,
                    Name = "api",
                    Description = "Connection properties for a 3rd party API"
                }
            };

            return connectionTypes;
        }

        public static List<Connection> GetConnections()
        {
            List<Connection> connections = new List<Connection>
            {
                new Connection
                {
                    ConnectionID = 1,
                    Name = "Binance US",
                    ConnectionTypeID = 1,
                    IsDataSource = true
                },
                new Connection
                {
                    ConnectionID = 2,
                    Name = "Coinbase",
                    ConnectionTypeID = 1,
                    IsDataSource = true
                },
                new Connection
                {
                    ConnectionID = 3,
                    Name = "Charles Schwab",
                    ConnectionTypeID = 1,
                    IsDataSource = false
                }
            };

            return connections;
        }
    }
}
