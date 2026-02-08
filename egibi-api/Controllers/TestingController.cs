using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class TestingController : ControllerBase
    {
        private readonly TestingService _testingService;

        public TestingController(TestingService testingService)
        {
            _testingService = testingService;
        }
    }
}
