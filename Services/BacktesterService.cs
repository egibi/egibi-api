#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
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
                        Value = s.Id.ToString()
                    }).ToListAsync();

                return new RequestResponse(dataSources, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> GetBacktests()
        {
            try
            {
                List<Backtest> backtests = await _db.Backtests
                    .ToListAsync();

                return new RequestResponse(backtests, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> GetBacktest(int backtestId)
        {
            try
            {
                var backtest = await _db.Backtests
                    .FirstOrDefaultAsync(x => x.Id == backtestId);
                return new RequestResponse(backtest, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> DeleteBacktest(int backtestId)
        {
            try
            {
                _db.Remove(_db.Backtests
                    .Where(w => w.Id == backtestId)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(backtestId, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(backtestId, 500, "Problem Deleting", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> DeleteBacktests(List<int> backtestIds)
        {
            try
            {
                _db.RemoveRange(_db.Backtests
                    .Where(w => backtestIds.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(backtestIds, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(backtestIds, 500, "Problem Deleting", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> SaveBacktest(Backtest backtest)
        {
            if (backtest.Id == 0)
                return await CreateNewBacktest(backtest);
            else
                return await UpdateExistingBacktest(backtest);
        }

        private async Task<RequestResponse> CreateNewBacktest(Backtest backtest)
        {
            Backtest newBacktest = new Backtest
            {
                Name = backtest.Name,
                Description = backtest.Description,
                Start = backtest.Start,
                End = backtest.End,
                ConnectionId = backtest.ConnectionId
            };

            try
            {
                await _db.AddAsync(newBacktest);
                await _db.SaveChangesAsync();

                return new RequestResponse(backtest, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        private async Task<RequestResponse> UpdateExistingBacktest(Backtest backtest)
        {
            Backtest existingBacktest = await _db.Backtests
                .Where(w => w.Id == backtest.Id)
                .FirstOrDefaultAsync();

            existingBacktest.Name = backtest.Name;
            existingBacktest.Description = backtest.Description;
            existingBacktest.Start = backtest.Start;
            existingBacktest.End = backtest.End;
            existingBacktest.ConnectionId = backtest.ConnectionId;

            try
            {
                _db.Update(existingBacktest);
                await _db.SaveChangesAsync();

                return new RequestResponse(backtest, 200, "OK");
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException?.Message;

                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }

        }
    }
}
