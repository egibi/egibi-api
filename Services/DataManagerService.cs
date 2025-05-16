#nullable disable
using egibi_api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EgibiQuestDB;
using EgibiCoreLibrary.Models;

namespace egibi_api.Services
{
    public class DataManagerService
    {
        private readonly EgibiDbContext _db;
        private readonly ConfigOptions _configOptions;

        public DataManagerService(EgibiDbContext db, IOptions<ConfigOptions> configOptions)
        {
            _db = db;
            _configOptions = configOptions.Value;
        }

        public async Task<RequestResponse> SaveFile(IFormFile file)
        {
            var ingester = new Ingester("connectionString");
            ingester.LoadCsv(file);

            return new RequestResponse(null, null,"OK");
        }
    }
}
