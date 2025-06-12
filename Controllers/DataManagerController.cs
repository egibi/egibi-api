#nullable disable
using CsvHelper;
using CsvHelper.Configuration;
using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using EgibiCoreLibrary.Models.QuestDbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Globalization;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataManagerController : ControllerBase
    {
        private readonly DataManagerService _dataManagerService;
        private readonly QuestDbService _questDbService;

        public DataManagerController(DataManagerService dataManagerService, QuestDbService questDbService)
        {
            _dataManagerService = dataManagerService;
            _questDbService = questDbService;
        }

        [HttpGet("get-data-providers")]
        public async Task<RequestResponse> GetDataProviders()
        {
            return await _dataManagerService.GetDataProviders();
        }

        [HttpGet("get-data-provider")]
        public async Task<RequestResponse> GetDataProvider(int id)
        {
            return await _dataManagerService.GetDataProvider(id);
        }

        [HttpPost("save-data-provider")]
        public async Task<RequestResponse> SaveDataProvider(DataProvider dataProvider)
        {
            return await _dataManagerService.SaveDataProvider(dataProvider);
        }

        [HttpDelete("delete-data-provider")]
        public async Task<RequestResponse> DeleteDataProvider(int id)
        {
            return await _dataManagerService.DeleteDataProvider(id);
        }

        [HttpGet("get-data-provider-types")]
        public async Task<RequestResponse> GetDataProviderTypes()
        {
            return await _dataManagerService.GetDataProviderTypes();
        }
        [HttpGet("get-data-frequency-types")]
        public async Task<RequestResponse> GetDataFrequencyTypes()
        {
            return await _dataManagerService.GetDataFrequencyTypes();
        }
        [HttpGet("get-data-format-types")]
        public async Task<RequestResponse> GetDataFormatTypes()
        {
            return await _dataManagerService.GetDataFormatTypes();
        }

        [HttpPost("drop-file")]
        [DisableRequestSizeLimit] // TODO: If moved to hosting provider, setup tool or something to upload directly
        public async Task<RequestResponse> DropFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                // return bad request
            }

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Optional settings
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true
            });

            await csv.ReadAsync();
            csv.ReadHeader();

            var test = csv.HeaderRecord;

            return new RequestResponse(test, 200, "OK");
        }

        //============================================================================================================================
        // QUESTDB OPERATIONS
        //============================================================================================================================

        [HttpGet("get-questdb-tables")]
        public async Task<RequestResponse> GetQuestDbTables()
        {
            return await _questDbService.GetTables();
        }

        [HttpPost("create-questdb-table")]
        public async Task<RequestResponse> CreateTable(QuestDbTable table)
        {
            return await _questDbService.CreateTable(table);
        }

        [HttpPost("drop-questdb-table")]
        public async Task<RequestResponse> DropTable(string tableName)
        {
            return await _questDbService.DropTable(tableName);
        }
    }
}
