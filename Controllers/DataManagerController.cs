#nullable disable
using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;

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
        public async Task DropFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                // return bad request
            }

            await _dataManagerService.SaveFile(file);
        }
    }
}
