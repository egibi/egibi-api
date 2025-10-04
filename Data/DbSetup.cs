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

        public static Country GetUsaStates(ICollection<CountryAdministrativeDivision> countryAdministrativeDivisionTypes)
        {
            Country country = new Country
            {
                Name = "United States",
                Description = "United States of America",
                CreatedAt = DateTime.Now.ToUniversalTime(),
                IsActive = true,
                Abbreviation = "USA",
                CountryAdministrativeDivisions = new List<CountryAdministrativeDivision> {

                new CountryAdministrativeDivision()
                {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Alabama",
                        Abbreviation = "AL",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Alaska",
                        Abbreviation = "AK",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Arizona",
                        Abbreviation = "AZ",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Arkansas",
                        Abbreviation = "AR",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "California",
                        Abbreviation = "CA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Colorado",
                        Abbreviation = "CO",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Connecticut",
                        Abbreviation = "CT",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Delaware",
                        Abbreviation = "DE",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Florida",
                        Abbreviation = "FL",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Georgia",
                        Abbreviation = "GA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Hawaii",
                        Abbreviation = "HI",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Idaho",
                        Abbreviation = "ID",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Illinois",
                        Abbreviation = "IL",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Indiana",
                        Abbreviation = "IN",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Iowa",
                        Abbreviation = "IA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Kansas",
                        Abbreviation = "KS",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Kentucky",
                        Abbreviation = "KY",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Louisiana",
                        Abbreviation = "LA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Maine",
                        Abbreviation = "ME",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Maryland",
                        Abbreviation = "MD",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Massachusetts",
                        Abbreviation = "MA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Michigan",
                        Abbreviation = "MI",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Minnesota",
                        Abbreviation = "MN",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Mississippi",
                        Abbreviation = "MS",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Missouri",
                        Abbreviation = "MO",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Montana",
                        Abbreviation = "MT",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Nebraska",
                        Abbreviation = "NE",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Nevada",
                        Abbreviation = "NV",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "New Hampshire",
                        Abbreviation = "NH",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "New Jersey",
                        Abbreviation = "NJ",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "New Mexico",
                        Abbreviation = "NM",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "New York",
                        Abbreviation = "NY",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "North Carolina",
                        Abbreviation = "NC",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "North Dakota",
                        Abbreviation = "ND",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Ohio",
                        Abbreviation = "OH",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Oklahoma",
                        Abbreviation = "OK",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Oregon",
                        Abbreviation = "OR",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Pennsylvania",
                        Abbreviation = "PA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Rhode Island",
                        Abbreviation = "RI",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "South Carolina",
                        Abbreviation = "SC",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "South Dakota",
                        Abbreviation = "SD",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Tennessee",
                        Abbreviation = "TN",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Texas",
                        Abbreviation = "TX",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Utah",
                        Abbreviation = "UT",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Vermont",
                        Abbreviation = "VT",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Virginia",
                        Abbreviation = "VA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Washington",
                        Abbreviation = "WA",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "West Virginia",
                        Abbreviation = "WV",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Wisconsin",
                        Abbreviation = "WI",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    },
                    new CountryAdministrativeDivision()
                    {
                        CountryAdministrativeDivisionType = new CountryAdministrativeDivisionType()
                        {
                            Id = 1
                        },
                        Name = "Wyoming",
                        Abbreviation = "WY",
                        TimeZones = null,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        IsActive = true
                    }
                }
            };

            return country;
        }

    }
}
