using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class EnvironmentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EnvironmentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("get-environment")]
        public IActionResult GetEnvironment()
        {
            var env = new
            {
                Name = _configuration["EgibiEnvironment:Name"] ?? "Unknown",
                Tag = _configuration["EgibiEnvironment:Tag"] ?? "UNK"
            };

            return Ok(env);
        }
    }
}
