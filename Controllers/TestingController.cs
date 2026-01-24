using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestingController
    {
        private readonly TestingService _testingService;

        public TestingController(TestingService testingService)
        {
            _testingService = testingService;
        }
    }
}
