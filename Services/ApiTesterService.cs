#nullable disable
using egibi_api.Data;
using Microsoft.Extensions.Options;
using EgibiBinanceUsSdk;
using EgibiCoreLibrary.Models;

namespace egibi_api.Services

{
    public class ApiTesterService
    {
        private readonly EgibiDbContext _db;
        private readonly ConfigOptions _configOptions;

        private readonly BinanceUsHttpClient _binanceUsHttpClient;


        public ApiTesterService(EgibiDbContext db, IOptions<ConfigOptions> configOptions, BinanceUsHttpClient binanceUsHttpClient)
        {
            _db = db;
            _configOptions = configOptions.Value;
            _binanceUsHttpClient = binanceUsHttpClient;
        }

        public async Task<RequestResponse> TestConnection()
        {
            var connection = _db.Connections.FirstOrDefault(x => x.Name == "Binance US");
            //var connection = _db.Connections.FirstOrDefault(x => x.Name == "Coinbase");
            string baseUrl = connection.BaseUrl;

            try
            {
                var testResult = await _binanceUsHttpClient.TestConnectivity(baseUrl);
            }
            catch(Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException?.Message;
            }

            return null;
        }

        public async Task<RequestResponse> GetServerTime()
        {
            RequestResponse requestResponse = new RequestResponse();

            var connection = _db.Connections.FirstOrDefault(x => x.Name == "Binance US");
            string baseUrl = connection.BaseUrl;

            try
            {
                var result = await _binanceUsHttpClient.GetServerTime(baseUrl);
                return new RequestResponse(result, 200, "OK");
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException?.Message;

                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
