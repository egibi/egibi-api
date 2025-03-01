#nullable disable
using egibi_api.Data;
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
    }
}
