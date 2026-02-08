#nullable disable
using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AppConfigurationsController : ControllerBase
    {
        private readonly AppConfigurationsService _configurationService;

        public AppConfigurationsController(AppConfigurationsService configurationService)
        {
            _configurationService = configurationService;
        }

        //=====================================================================================
        // ENTITY TYPE OPERATIONS
        //=====================================================================================
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

        //=====================================================================================
        // ACCOUNT USER OPERATIONS
        //=====================================================================================
        [HttpGet("get-account-users")]
        public async Task<RequestResponse> GetAccountUsers()
        {
            return await _configurationService.GetAccountUsers();
        }

        [HttpPost("save-account-user")]
        public async Task<RequestResponse> SaveAccountUser([FromBody] AccountUser accountUser)
        {
            return await _configurationService.SaveAccountUser(accountUser);
        }

        [HttpPost("delete-account-user")]
        public async Task<RequestResponse> DeleteAccountUser(AccountUser accountUser)
        {
            return await _configurationService.DeleteAccountUser(accountUser);
        }

        //=====================================================================================
        // GEO DATETIME DATA
        //=====================================================================================


        [HttpGet("get-country-data")]
        public async Task<RequestResponse> GetCountydata()
        {
            return await _configurationService.GetCountryData();
        }

        [HttpGet("get-timezone-data")]
        public async Task<RequestResponse> GetTimeZoneData()
        {
            return await _configurationService.GetTimeZoneData();
        }


    }
}
