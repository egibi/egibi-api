// FILE: egibi-api/Data/DbSetup.cs
using egibi_api.Data.Entities;
using EgibiGeoDateTimeDataLibrary.Models;
using Country = egibi_api.Data.Entities.Country;
using TimeZone = egibi_api.Data.Entities.TimeZone;

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
                // ===== CRYPTO EXCHANGES =====
                new Connection
                {
                    Id = 1,
                    Name = "Binance US",
                    Description = "US-regulated cryptocurrency exchange with 100+ trading pairs",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "crypto_exchange",
                    IconKey = "binance",
                    Color = "#F0B90B",
                    Website = "https://www.binance.us",
                    DefaultBaseUrl = "https://api.binance.us",
                    RequiredFields = "[\"api_key\",\"api_secret\"]",
                    SortOrder = 1,
                    LinkMethod = "api_key"
                },
                new Connection
                {
                    Id = 2,
                    Name = "Coinbase",
                    Description = "Leading US cryptocurrency exchange with advanced trading features",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "crypto_exchange",
                    IconKey = "coinbase",
                    Color = "#0052FF",
                    Website = "https://www.coinbase.com",
                    DefaultBaseUrl = "https://api.coinbase.com",
                    RequiredFields = "[\"api_key\",\"api_secret\"]",
                    SortOrder = 2,
                    LinkMethod = "api_key"
                },
                new Connection
                {
                    Id = 3,
                    Name = "Coinbase Pro",
                    Description = "Coinbase advanced trading platform with lower fees and API access",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "crypto_exchange",
                    IconKey = "coinbase",
                    Color = "#0052FF",
                    Website = "https://pro.coinbase.com",
                    DefaultBaseUrl = "https://api.pro.coinbase.com",
                    RequiredFields = "[\"api_key\",\"api_secret\",\"passphrase\"]",
                    SortOrder = 3,
                    LinkMethod = "api_key"
                },
                new Connection
                {
                    Id = 4,
                    Name = "Kraken",
                    Description = "Global cryptocurrency exchange with margin trading and staking",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "crypto_exchange",
                    IconKey = "kraken",
                    Color = "#7132F5",
                    Website = "https://www.kraken.com",
                    DefaultBaseUrl = "https://api.kraken.com",
                    RequiredFields = "[\"api_key\",\"api_secret\"]",
                    SortOrder = 4,
                    LinkMethod = "api_key"
                },

                // ===== STOCK BROKERS =====
                new Connection
                {
                    Id = 5,
                    Name = "Charles Schwab",
                    Description = "Full-service brokerage with stocks, ETFs, options, and futures",
                    ConnectionTypeId = 2,
                    IsDataSource = false,
                    IsActive = true,
                    Category = "stock_broker",
                    IconKey = "schwab",
                    Color = "#00A0DF",
                    Website = "https://www.schwab.com",
                    DefaultBaseUrl = "https://api.schwabapi.com",
                    RequiredFields = "[\"api_key\",\"api_secret\"]",
                    SortOrder = 10,
                    LinkMethod = "api_key"
                },
                new Connection
                {
                    Id = 6,
                    Name = "Alpaca",
                    Description = "Commission-free API-first stock and crypto trading",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "stock_broker",
                    IconKey = "alpaca",
                    Color = "#FFCD00",
                    Website = "https://alpaca.markets",
                    DefaultBaseUrl = "https://paper-api.alpaca.markets",
                    RequiredFields = "[\"api_key\",\"api_secret\"]",
                    SortOrder = 11,
                    LinkMethod = "api_key"
                },
                new Connection
                {
                    Id = 7,
                    Name = "Interactive Brokers",
                    Description = "Professional-grade brokerage for stocks, options, forex, and futures",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "stock_broker",
                    IconKey = "ibkr",
                    Color = "#D81B2C",
                    Website = "https://www.interactivebrokers.com",
                    DefaultBaseUrl = "https://localhost:5000/v1/api",
                    RequiredFields = "[\"username\",\"password\"]",
                    SortOrder = 12,
                    LinkMethod = "api_key"
                },

                // ===== DATA PROVIDERS =====
                new Connection
                {
                    Id = 8,
                    Name = "Alpha Vantage",
                    Description = "Free and premium stock, forex, and crypto market data APIs",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "data_provider",
                    IconKey = "alphavantage",
                    Color = "#7B42BC",
                    Website = "https://www.alphavantage.co",
                    DefaultBaseUrl = "https://www.alphavantage.co/query",
                    RequiredFields = "[\"api_key\"]",
                    SortOrder = 20,
                    LinkMethod = "api_key"
                },
                new Connection
                {
                    Id = 9,
                    Name = "Polygon.io",
                    Description = "Real-time and historical market data for stocks, options, forex, and crypto",
                    ConnectionTypeId = 2,
                    IsDataSource = true,
                    IsActive = true,
                    Category = "data_provider",
                    IconKey = "polygon",
                    Color = "#7950F2",
                    Website = "https://polygon.io",
                    DefaultBaseUrl = "https://api.polygon.io",
                    RequiredFields = "[\"api_key\"]",
                    SortOrder = 21,
                    LinkMethod = "api_key"
                },

                // ===== FUNDING PROVIDERS =====
                new Connection
                {
                    Id = 10,
                    Name = "Mercury",
                    Description = "Business banking with powerful API access for programmatic fund management",
                    ConnectionTypeId = 2,
                    IsDataSource = false,
                    IsActive = true,
                    Category = "funding_provider",
                    IconKey = "mercury",
                    Color = "#6366F1",
                    Website = "https://mercury.com",
                    DefaultBaseUrl = "https://api.mercury.com/api/v1",
                    RequiredFields = "[\"api_key\"]",
                    SortOrder = 30,
                    SignupUrl = "https://app.mercury.com/signup",
                    ApiDocsUrl = "https://docs.mercury.com",
                    LinkMethod = "api_key"
                },
                new Connection
                {
                    Id = 11,
                    Name = "Plaid",
                    Description = "Connect any US bank account securely via Plaid Link",
                    ConnectionTypeId = 2,
                    IsDataSource = false,
                    IsActive = true,
                    Category = "funding_provider",
                    IconKey = "plaid",
                    Color = "#00D09C",
                    Website = "https://plaid.com",
                    DefaultBaseUrl = "https://production.plaid.com",
                    RequiredFields = "[]",
                    SortOrder = 31,
                    SignupUrl = "",
                    ApiDocsUrl = "https://plaid.com/docs",
                    LinkMethod = "plaid_link"
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

        public static List<BacktestStatus> GetBacktestStatuses()
        {
            List<BacktestStatus> backtestStatuses = new List<BacktestStatus>
            {
                new BacktestStatus
                {
                    Id = 1,
                    Name = "Pending",
                    Description = "Backtest is queued and waiting to execute",
                    IsActive = true,
                },
                new BacktestStatus
                {
                    Id = 2,
                    Name = "Running",
                    Description = "Backtest is currently executing",
                    IsActive = true,
                },
                new BacktestStatus
                {
                    Id = 3,
                    Name = "Completed",
                    Description = "Backtest finished successfully",
                    IsActive = true,
                },
                new BacktestStatus
                {
                    Id = 4,
                    Name = "Failed",
                    Description = "Backtest encountered an error during execution",
                    IsActive = true,
                },
                new BacktestStatus
                {
                    Id = 5,
                    Name = "Cancelled",
                    Description = "Backtest was cancelled before completion",
                    IsActive = true,
                }
            };
            return backtestStatuses;
        }

        public static List<Exchange> GetExchanges()
        {
            // Exchange entity is for exchange metadata (fee structures, etc.)
            // Connection entity is now the primary service catalog
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

        public static List<CountryAdministrativeDivisionType> GetCountryAdministrativeDivisionTypes()
        {
            List<CountryAdministrativeDivisionType> countryAdministrativeDivisionTypes = new List<CountryAdministrativeDivisionType>
            {
                new CountryAdministrativeDivisionType()
                {
                    Id = 1,
                    Name = "State",
                    CreatedAt = DateTime.Now.ToUniversalTime(),
                    IsActive = true
                }
            };
            return countryAdministrativeDivisionTypes;
        }

        public static List<Country> GetCountryData()
        {
            List<Country> countryEntities = new List<Country>();
            List<CountryData> countryData = new EgibiGeoDateTimeDataLibrary.LoadCountryData().Load();

            if (countryData != null && countryData.Count > 0)
            {
                int id = 1;
                countryData.ForEach(cd =>
                {
                    Country countryRecord = new Country()
                    {
                        Id = id,
                        CountryCode = cd.CountryCode,
                        CountryName = cd.CountryName,
                        IsActive = true,
                    };

                    countryEntities.Add(countryRecord);
                    id++;
                });
            }

            return countryEntities;
        }

        public static List<TimeZone> GetTimeZoneData()
        {
            List<TimeZone> timeZoneEntities = new List<TimeZone>();
            List<TimeZoneData> timeZoneData = new EgibiGeoDateTimeDataLibrary.LoadTimeZoneData().Load();

            if (timeZoneData != null && timeZoneData.Count > 0)
            {
                int id = 1;
                //TODO: Output new record creation during seeding process
                timeZoneData.ForEach(tzd =>
                {
                    TimeZone timeZoneRecord = new TimeZone()
                    {
                        Id = id,
                        Abbreviation = tzd.Abbreviation,
                        CountryCode = tzd.CountryCode,
                        IsActive = true,
                        Dst = EgibiGeoDateTimeDataLibrary.Utilities.ConvertDst(tzd.Dst),
                        GmtOffset = null,
                        TimeStart = null
                    };

                    id++;
                    timeZoneEntities.Add(timeZoneRecord);
                });
            }

            return timeZoneEntities;
        }
    }
}
