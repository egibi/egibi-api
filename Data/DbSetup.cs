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
                    Id = 1,
                    Name = "unknown",
                    Description = "unknown connection type",
                    IsActive = true,
                },
                new ConnectionType
                {
                    Id = 2,
                    Name = "api",
                    Description = "Connection properties for a 3rd party API",
                    IsActive = true,
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
                    Id = 1,
                    Name = "Binance US",
                    ConnectionTypeId = 1,
                    IsDataSource = true,
                    IsActive = true,

                },
                new Connection
                {
                    Id = 2,
                    Name = "Coinbase",
                    ConnectionTypeId = 1,
                    IsDataSource = true,
                    IsActive = true,
                },
                new Connection
                {
                    Id = 3,
                    Name = "Charles Schwab",
                    ConnectionTypeId = 1,
                    IsDataSource = false,
                    IsActive = true,
                }
            };

            return connections;
        }

        public static List<DataProviderType> GetDataProviderTypes()
        {
            List<DataProviderType> dataProviderTypes = new List<DataProviderType>
            {
                new DataProviderType
                {
                    Id = 1,
                    Name = "File",
                    Description = "Imported data file",
                    IsActive = true,
                },
                new DataProviderType
                {
                    Id = 2,
                    Name = "API",
                    Description = "3rd party API connection",
                    IsActive = true,
                },
                new DataProviderType
                {
                    Id = 3,
                    Name = "Websocket",
                    Description = "3rd party websocket connection",
                    IsActive = true,
                },
                new DataProviderType
                {
                    Id = 4,
                    Name = "LLM",
                    Description = "3rd party LLM prompt responses",
                    IsActive = true,
                }
            };

            return dataProviderTypes;
        }

        public static List<DataFrequencyType> GetDataFrequencyTypes()
        {
            List<DataFrequencyType> dataFrequencyTypes = new List<DataFrequencyType>
            {
                new DataFrequencyType
                {
                    Id = 1,
                    Name = "second",
                    Description = "Data point for every second",
                    IsActive = true,
                },
                new DataFrequencyType
                {
                    Id = 2,
                    Name = "minute",
                    Description = "Data point for every minute",
                    IsActive = true,
                },
                new DataFrequencyType
                {
                    Id = 3,
                    Name = "hour",
                    Description = "Data point for every hour",
                    IsActive = true,
                },
                new DataFrequencyType
                {
                    Id = 4,
                    Name = "day",
                    Description = "Data point for every day",
                    IsActive = true,
                }
            };

            return dataFrequencyTypes;
        }

        public static List<DataFormatType> GetDataFormatTypes()
        {
            List<DataFormatType> dataFormatTypes = new List<DataFormatType>
            {
                new DataFormatType
                {
                    Id = 1,
                    Name = "OHLC",
                    Description = "Open, High, Low, Close",
                    IsActive = true,
                }
            };

            return dataFormatTypes;
        }

        public static List<Exchange> GetExchanges()
        {
            List<Exchange> exchanges = new List<Exchange>
            {
                new Exchange
                {
                    Id = 1,
                    Name = "Coinbase",
                    Description = ""
                }
            };

            return null;
        }

        public static List<ExchangeFeeStructure> GetExchangeFeeStructures()
        {
            return null;
        }

        public static List<ExchangeFeeStructureTier> GetExchangeFeeStructureTiers()
        {
            return null;
        }

    }
}
