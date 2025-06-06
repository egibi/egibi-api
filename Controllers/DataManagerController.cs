#nullable disable
using CsvHelper;
using CsvHelper.Configuration;
using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using EgibiCoreLibrary.Models.QuestDbModels;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataManagerController : ControllerBase
    {
        private readonly DataManagerService _dataManagerService;

        public DataManagerController(DataManagerService dataManagerService)
        {
            _dataManagerService = dataManagerService;
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


        [HttpPost("create-questdb-table")]
        public async Task<RequestResponse> CreateQuestDbTable()
        {
            List<Ohlcv> data = new List<Ohlcv>()
            {
               new Ohlcv
               {
                   TimeStamp = DateTime.Now.ToUniversalTime(),
                   DateTime = DateTime.Now.ToUniversalTime(),
                   Open = 0.123M,
                   High = 1.0123M,
                   Close = 1.02M,
                   Volume = 123409.40982M,
               },
               new Ohlcv
               {
                   TimeStamp = DateTime.Now.ToUniversalTime(),
                   DateTime = DateTime.Now.ToUniversalTime(),
                   Open = 0.124M,
                   High = 1.0125M,
                   Close = 1.03M,
                   Volume = 123409.44482M,
               },
               new Ohlcv
               {
                   TimeStamp = DateTime.Now.ToUniversalTime(),
                   DateTime = DateTime.Now.ToUniversalTime(),
                   Open = 0.125M,
                   High = 1.0125M,
                   Close = 1.04M,
                   Volume = 123409.4032M,
               },
            };

            await _dataManagerService.CreateQuestDbTable(data);

            var response = new RequestResponse("testValue", 202, "this is a test response");

            return response;
        }
    }
}
