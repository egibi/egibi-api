#nullable disable
using egibi_api.Data;
using EgibiCoreLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace egibi_api.Services
{
    public class BacktesterService
    {

        private readonly EgibiDbContext _db;
        private readonly ConfigOptions _configOptions;

        public BacktesterService(EgibiDbContext db, IOptions<ConfigOptions> configOptions)
        {
            _db = db;
            _configOptions = configOptions.Value;
        }

        public async Task<RequestResponse> GetDataSources()
        {
            try
            {
                List<SelectOptionModel> dataSources = await _db.Connections
                    .Select(s => new SelectOptionModel()
                    {
                        Text = s.Name,
                        Value = s.ConnectionID.ToString()
                    }).ToListAsync();

                return new RequestResponse(dataSources, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
