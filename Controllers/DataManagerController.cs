#nullable disable
using egibi_api.Data.Entities;
using egibi_api.Services;
using EgibiCoreLibrary;
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

        [HttpPost("drop-file")]
        public async Task DropFile(IFormFile file)
        {
            if(file == null || file.Length == 0)
            {
                // return bad request
            }            
        }
    }
}
