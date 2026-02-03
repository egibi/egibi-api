using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using QuestDB;


namespace EgibiQuestDB
{
    public class Ingester
    {
        private readonly string _connectionString;

        public Ingester(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task LoadCsv(IFormFile file)
        {
            try
            {
                using var sender = Sender.New("http::addr=localhost:9000;");
                await sender.Table("trades")
                    .Symbol("symbol", "BTC-USD")
                    .Column("open", "open")
                    .Column("high", 2615.54)
                    .Column("low", 0.00044)
                    .Column("close", 2345.54)
                    .Column("volume", 0)
                    .Column("datetime", "1/1/2012 4:01:00 AM")
                    .AtAsync(DateTime.Now);
                //await sender.SendAsync();
                sender.Send();
            }
            catch (Exception ex)
            {
                var exception = ex;
            }

        }
    }
}
