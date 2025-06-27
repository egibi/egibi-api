#nullable disable
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ConfigurationService _configurationService;

        public ConfigurationController(ConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        [HttpGet("get-entity-type-tables")]
        public async Task<RequestResponse> GetEntityTypes()
        {
            return await _configurationService.GetEntityTypeTables();
        }

        [HttpGet("get-entity-type-records")]
        public async Task<RequestResponse> GetEntityTypeRecords(string tableName)
        {
            return await _configurationService.GetEntityTypeRecords(tableName);
        }
    }
}
