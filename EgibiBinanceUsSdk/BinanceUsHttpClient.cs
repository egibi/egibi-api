#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EgibiBinanceUsSdk
{
    public class BinanceUsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _secret;

        public BinanceUsHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;            
        }



        public async Task<string> TestConnectivity(string baseUrl)
        {
            string endpoint = "/api/v3/ping";
           
            try
            {
                string response = await _httpClient.GetStringAsync($"{baseUrl}{endpoint}");
                return response;
            }
            catch(Exception ex)
            {
                return $"Exception: {ex.Message} | InnerException: {ex.InnerException?.Message}";
            }            
        }

        public async Task<string> GetServerTime(string baseUrl)
        {
            string endpoint = "/api/v3/time";

            try
            {
                string response = await _httpClient.GetStringAsync($"{baseUrl}{endpoint}");
                return response;
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message} | InnerException: {ex.InnerException?.Message}";
            }
        }
    }
}
