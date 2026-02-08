#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Services
{
    public class ExchangeAccountsService
    {
        private readonly EgibiDbContext _db;

        public ExchangeAccountsService(EgibiDbContext db)
        {
            _db = db;
        }

        public async Task<RequestResponse> GetExchangeAccounts()
        {
            try
            {
                List<ExchangeAccount> exchangeAccounts = await _db.ExchangeAccounts
                    .ToListAsync();

                return new RequestResponse(exchangeAccounts, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> GetExchangeAccount(int id)
        {
            try
            {
                var exchangeAccount = await _db.ExchangeAccounts
                    .Where(w => w.Id == id)
                    .FirstOrDefaultAsync();

                return new RequestResponse(exchangeAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> SaveExchangeAccount(ExchangeAccount exchangeAccount)
        {
            if (exchangeAccount.Id == 0)
                return await CreateNewExchangeAccount(exchangeAccount);
            else
                return await UpdateExistingExchangeAccount(exchangeAccount);
        }
        public async Task<RequestResponse> DeleteExchangeAccount(int id)
        {
            try
            {
                // FIX #5: Was using FirstOrDefaultAsync() which returns Task, not entity.
                // Changed to synchronous FirstOrDefault() so _db.Remove() gets the actual entity.
                var entity = _db.ExchangeAccounts
                    .Where(w => w.Id == id)
                    .FirstOrDefault();

                if (entity == null)
                    return new RequestResponse(id, 404, "Exchange account not found");

                _db.Remove(entity);
                await _db.SaveChangesAsync();

                return new RequestResponse(id, 200, "Deleted");
            }
            catch(Exception ex)
            {
                return new RequestResponse(id, 500, "Problem Deleting", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> DeleteExchangeAccounts(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.ExchangeAccounts
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "Deleted");
            }
            catch(Exception ex)
            {
                return new RequestResponse(ids, 500, "Problem Deleting", new ResponseError(ex));
            }
        }


        private async Task<RequestResponse> CreateNewExchangeAccount(ExchangeAccount exchangeAccount)
        {
            // FIX #3: Removed plaintext Username/Password copy.
            // Credentials should be stored via UserCredential with per-user encryption.
            ExchangeAccount newExchangeAccount = new ExchangeAccount
            {
                Name = exchangeAccount.Name,
                Description = exchangeAccount.Description,
                Notes = exchangeAccount.Notes,
                CreatedAt = DateTime.UtcNow, // FIX #14: Use DateTime.UtcNow directly
                IsActive = true,
            };

            try
            {
                await _db.AddAsync(newExchangeAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(newExchangeAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        private async Task<RequestResponse> UpdateExistingExchangeAccount(ExchangeAccount exchangeAccount)
        {
            try
            {
                ExchangeAccount existingExchangeAccount = await _db.ExchangeAccounts
                    .Where(w => w.Id == exchangeAccount.Id)
                    .FirstOrDefaultAsync();

                if (existingExchangeAccount == null)
                    return new RequestResponse(null, 404, "Exchange account not found");

                existingExchangeAccount.Name = exchangeAccount.Name;
                existingExchangeAccount.Description = exchangeAccount.Description;
                existingExchangeAccount.Notes = exchangeAccount.Notes;
                existingExchangeAccount.IsActive = exchangeAccount.IsActive;
                // FIX #3: Removed plaintext Username/Password copy
                existingExchangeAccount.AssetBalance = exchangeAccount.AssetBalance;
                existingExchangeAccount.CurrentSpotVolume_30Day = exchangeAccount.CurrentSpotVolume_30Day;
                existingExchangeAccount.LastModifiedAt = DateTime.UtcNow; // FIX #14

                _db.Update(existingExchangeAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(existingExchangeAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }

        }
    }
}
