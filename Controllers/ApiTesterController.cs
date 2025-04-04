#nullable disable
using egibi_api.Data.Entities;
using egibi_api.Services;
using Microsoft.AspNetCore.Mvc;
using EgibiCoreLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiTesterController
    {
        private readonly ApiTesterService _apiTesterService;

        public ApiTesterController(ApiTesterService apiTesterService)
        {
            _apiTesterService = apiTesterService;
        }

        [HttpGet("test-connection")]
        public async Task<RequestResponse> TestConnection()
        {
            return await _apiTesterService.TestConnection();
        }

        [HttpGet("get-server-time")]
        public async Task<RequestResponse> GetServerTime()
        {
            return await _apiTesterService.GetServerTime();
        }



    }
}
