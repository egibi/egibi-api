#nullable disable
using egibi_api.Data;
using EgibiCoreLibrary;
using Microsoft.Extensions.Options;
namespace egibi_api.Services

{
    public class ApiTesterService
    {
        private readonly EgibiDbContext _db;
        private readonly ConfigOptions _configOptions;

        public ApiTesterService(EgibiDbContext db, IOptions<ConfigOptions> configOptions)
        {
            _db = db;
            _configOptions = configOptions.Value;
        }

        public async Task<RequestResponse> TestConnection()
        {
            try
            {
                return new RequestResponse("Initial Test OK", 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
            
        }
    }
}
