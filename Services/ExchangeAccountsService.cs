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
                _db.Remove(_db.ExchangeAccounts
                    .Where(w => w.Id == id)
                    .FirstOrDefaultAsync());
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
            ExchangeAccount newExchangeAccount = new ExchangeAccount
            {
                Name = exchangeAccount.Name,
                Description = exchangeAccount.Description,
                Notes = exchangeAccount.Notes,
                CreatedAt = DateTime.Now.ToUniversalTime(),
                IsActive = true,
                Username = exchangeAccount.Username,
                Password = exchangeAccount.Password,
            };

            try
            {
                await _db.AddAsync(newExchangeAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(exchangeAccount, 200, "OK");
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

                existingExchangeAccount.Id = exchangeAccount.Id;
                existingExchangeAccount.Name = exchangeAccount.Name;
                existingExchangeAccount.Description = exchangeAccount.Description;
                existingExchangeAccount.Notes = exchangeAccount.Notes;
                existingExchangeAccount.IsActive = exchangeAccount.IsActive;
                existingExchangeAccount.Username = exchangeAccount.Username;
                existingExchangeAccount.Password = exchangeAccount.Password;
                existingExchangeAccount.AssetBalance = exchangeAccount.AssetBalance;
                existingExchangeAccount.CurrentSpotVolume_30Day = exchangeAccount.CurrentSpotVolume_30Day;
                existingExchangeAccount.LastModifiedAt = DateTime.Now.ToUniversalTime();

                _db.Update(existingExchangeAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(exchangeAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }

        }
    }
}
