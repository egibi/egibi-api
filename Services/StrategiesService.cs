#nullable disable
using egibi_api.Data;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Strategy = egibi_api.Data.Entities.Strategy;

namespace egibi_api.Services
{
    public class StrategiesService
    {
        private readonly EgibiDbContext _db;
        private readonly ConfigOptions _configOptions;

        public StrategiesService(EgibiDbContext db, IOptions<ConfigOptions> configOptions)
        {
            _db = db;
            _configOptions = configOptions.Value;
        }

        public async Task<RequestResponse> GetStrategies()
        {
            try
            {
                List<Strategy> strategies = await _db.Strategies
                    .ToListAsync();

                return new RequestResponse(strategies, 200, "OK");
            }
            catch(Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetStrategy(int id)
        {
            try
            {
                var strategy = await _db.Strategies
                    .FirstOrDefaultAsync(x => x.Id == id);
                return new RequestResponse(strategy, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteStrategy(int id)
        {
            try
            {
                _db.Remove(_db.Strategies
                    .Where(w => w.Id == id)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(id, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(id, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteStrategies(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.Strategies
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "Deleted");
            }
            catch(Exception ex)
            {
                return new RequestResponse(ids, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveStrategy(Strategy strategy)
        {
            if (strategy.Id == 0)
                return await CreateNewStrategy(strategy);
            else
                return await UpdateExistingStrategy(strategy);
        }

        private async Task<RequestResponse> CreateNewStrategy(Strategy strategy)
        {
            Strategy newStrategy = new Strategy
            {
                Name = strategy.Name,
                Description = strategy.Description,
                InstanceName = strategy.InstanceName
            };

            try
            {
                await _db.AddAsync(newStrategy);
                await _db.SaveChangesAsync();

                return new RequestResponse(strategy, 200, "OK");
            }
            catch(Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateExistingStrategy(Strategy strategy)
        {
            Strategy existingStrategy = await _db.Strategies
                .Where(w => w.Id == strategy.Id)
                .FirstOrDefaultAsync();

            existingStrategy.Name = strategy.Name;
            existingStrategy.Description = strategy.Description;
            existingStrategy.InstanceName = strategy.InstanceName;

            try
            {
                _db.Update(existingStrategy);
                await _db.SaveChangesAsync();

                return new RequestResponse(strategy, 200, "OK");
            }
            catch(Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException?.Message;

                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
