#nullable disable
using egibi_api.Data;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Market = egibi_api.Data.Entities.Market;

namespace egibi_api.Services
{
    public class MarketsService
    {
        private readonly EgibiDbContext _db;

        public MarketsService(EgibiDbContext db)
        {
            _db = db;
        }

        public async Task<RequestResponse> GetMarkets()
        {
            try
            {
                List<Market> markets = await _db.Markets
                    .Include("MarketType")
                    .ToListAsync();

                return new RequestResponse(markets, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetMarket(int id)
        {
            try
            {
                var market = await _db.Markets
                    .Include("MarketType")
                    .FirstOrDefaultAsync(x => x.Id == id);

                return new RequestResponse(market, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetMarketTypes()
        {
            try
            {
                var marketTypes = await _db.MarketTypes.ToListAsync();

                return new RequestResponse(marketTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveMarket(Market market)
        {
            try
            {
                if (market.Id == 0)
                    return await CreateNewMarket(market);
                else
                    return await UpdateExistingMarket(market);
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteMarket(int id)
        {
            try
            {
                _db.Remove(_db.Markets
                    .Where(w => w.Id == id)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(id, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteMarkets(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.Markets
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> CreateNewMarket(Market market)
        {
            Market newMarket = new Market
            {
                Name = market.Name,
                Description = market.Description,
                Notes = market.Notes,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            try
            {
                await _db.AddAsync(newMarket);
                await _db.SaveChangesAsync();

                return new RequestResponse(newMarket, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateExistingMarket(Market market)
        {
            try
            {
                Market existingMarket = await _db.Markets
                    .Where(w => w.Id == market.Id)
                    .FirstOrDefaultAsync();

                existingMarket.Name = market.Name;
                existingMarket.Description = market.Description;
                existingMarket.Notes = market.Notes;
                existingMarket.IsActive = market.IsActive;
                existingMarket.LastModifiedAt = DateTime.Now;

                _db.Update(existingMarket);
                await _db.SaveChangesAsync();

                return new RequestResponse(market, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
