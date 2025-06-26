#nullable disable
using egibi_api.Data;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Exchange = egibi_api.Data.Entities.Exchange;

namespace egibi_api.Services
{
    public class ExchangesService
    {
        private readonly EgibiDbContext _db;
        public ExchangesService(EgibiDbContext db)
        {
            _db = db;
        }

        public async Task<RequestResponse> GetExchanges()
        {
            try
            {
                List<Exchange> exchanges = await _db.Exchanges
                    .Include("ExchangeType")
                    .ToListAsync();

                return new RequestResponse(exchanges, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetExchange(int id)
        {
            try
            {
                var exchange = await _db.Exchanges
                    .Include("ExchangeType")
                    .FirstOrDefaultAsync(x => x.Id == id);

                return new RequestResponse(exchange, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetExchangeTypes()
        {
            try
            {
                var exchangeTypes = await _db.ExchangeTypes.ToListAsync();

                return new RequestResponse(exchangeTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveExchange(Exchange exchange)
        {
            try
            {
                if (exchange.Id == 0)
                    return await CreateNewExchange(exchange);
                else
                    return await UpdateExistingExchange(exchange);
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteExchange(int id)
        {
            try
            {
                _db.Remove(_db.Exchanges
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

        public async Task<RequestResponse> DeleteExchanges(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.Exchanges
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> CreateNewExchange(Exchange exchange)
        {
            Exchange newExchange = new Exchange
            {
                Name = exchange.Name,
                Description = exchange.Description,
                Notes = exchange.Notes,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            try
            {
                await _db.AddAsync(newExchange);
                await _db.SaveChangesAsync();

                return new RequestResponse(newExchange, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateExistingExchange(Exchange exchange)
        {           
            try
            {
                Exchange existingExchange = await _db.Exchanges
                    .Where(w => w.Id == exchange.Id)
                    .FirstOrDefaultAsync();

                existingExchange.Name = exchange.Name;
                existingExchange.Description = exchange.Description;
                existingExchange.Notes = exchange.Notes;
                existingExchange.IsActive = exchange.IsActive;
                existingExchange.LastModifiedAt = DateTime.Now;

                _db.Update(existingExchange);
                await _db.SaveChangesAsync();

                return new RequestResponse(exchange, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

    }
}
