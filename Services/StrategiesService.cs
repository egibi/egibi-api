#nullable disable
using egibi_api.Data;
using EgibiCoreLibrary;
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

        public async Task<RequestResponse> GetStrategy(int strategyId)
        {
            try
            {
                var strategy = await _db.Strategies
                    .FirstOrDefaultAsync(x => x.StrategyID == strategyId);
                return new RequestResponse(strategy, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteStrategy(int strategyId)
        {
            try
            {
                _db.Remove(_db.Strategies
                    .Where(w => w.StrategyID == strategyId)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(strategyId, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(strategyId, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteStrategies(List<int> strategyIds)
        {
            try
            {
                _db.RemoveRange(_db.Strategies
                    .Where(w => strategyIds.Contains(w.StrategyID)));
                await _db.SaveChangesAsync();

                return new RequestResponse(strategyIds, 200, "Deleted");
            }
            catch(Exception ex)
            {
                return new RequestResponse(strategyIds, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveStrategy(Strategy strategy)
        {
            if (strategy.StrategyID == 0)
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
                .Where(w => w.StrategyID == strategy.StrategyID)
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
