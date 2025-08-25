#nullable disable
using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppConfigurationsController : ControllerBase
    {
        private readonly AppConfigurationsService _configurationService;

        public AppConfigurationsController(AppConfigurationsService configurationService)
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

        [HttpPost("save-entity-type")]
        public async Task<RequestResponse> SaveEntityType([FromBody] EntityType entityType)
        {
            return await _configurationService.SaveEntityType(entityType);
        }

        [HttpPost("delete-entity-type")]
        public async Task<RequestResponse> DeleteEntityType([FromBody] EntityType entityType)
        {
            return await _configurationService.DeleteEntityType(entityType);
        }
    }
}
