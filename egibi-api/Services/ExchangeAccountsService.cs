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
        private readonly ILogger<ExchangeAccountsService> _logger;

        public ExchangeAccountsService(EgibiDbContext db, ILogger<ExchangeAccountsService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // FIX F-02: All methods now require appUserId and scope queries accordingly.

        public async Task<RequestResponse> GetExchangeAccounts(int appUserId)
        {
            try
            {
                List<ExchangeAccount> exchangeAccounts = await _db.ExchangeAccounts
                    .Where(ea => ea.AppUserId == appUserId)
                    .ToListAsync();

                return new RequestResponse(exchangeAccounts, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exchange accounts for user {UserId}", appUserId);
                return new RequestResponse(null, 500, "There was an error retrieving exchange accounts.");
            }
        }

        public async Task<RequestResponse> GetExchangeAccount(int id, int appUserId)
        {
            try
            {
                var exchangeAccount = await _db.ExchangeAccounts
                    .Where(ea => ea.Id == id && ea.AppUserId == appUserId)
                    .FirstOrDefaultAsync();

                if (exchangeAccount == null)
                    return new RequestResponse(null, 404, "Exchange account not found");

                return new RequestResponse(exchangeAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exchange account {Id} for user {UserId}", id, appUserId);
                return new RequestResponse(null, 500, "There was an error retrieving the exchange account.");
            }
        }

        public async Task<RequestResponse> SaveExchangeAccount(ExchangeAccount exchangeAccount, int appUserId)
        {
            if (exchangeAccount.Id == 0)
                return await CreateNewExchangeAccount(exchangeAccount, appUserId);
            else
                return await UpdateExistingExchangeAccount(exchangeAccount, appUserId);
        }

        public async Task<RequestResponse> DeleteExchangeAccount(int id, int appUserId)
        {
            try
            {
                var entity = await _db.ExchangeAccounts
                    .Where(ea => ea.Id == id && ea.AppUserId == appUserId)
                    .FirstOrDefaultAsync();

                if (entity == null)
                    return new RequestResponse(id, 404, "Exchange account not found");

                _db.Remove(entity);
                await _db.SaveChangesAsync();

                return new RequestResponse(id, 200, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exchange account {Id} for user {UserId}", id, appUserId);
                return new RequestResponse(id, 500, "Problem deleting exchange account.");
            }
        }

        public async Task<RequestResponse> DeleteExchangeAccounts(List<int> ids, int appUserId)
        {
            try
            {
                // Only delete accounts that belong to this user
                var entities = await _db.ExchangeAccounts
                    .Where(ea => ids.Contains(ea.Id) && ea.AppUserId == appUserId)
                    .ToListAsync();

                if (entities.Count == 0)
                    return new RequestResponse(ids, 404, "No matching exchange accounts found");

                _db.RemoveRange(entities);
                await _db.SaveChangesAsync();

                var deletedIds = entities.Select(e => e.Id).ToList();
                return new RequestResponse(deletedIds, 200, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exchange accounts for user {UserId}", appUserId);
                return new RequestResponse(ids, 500, "Problem deleting exchange accounts.");
            }
        }


        private async Task<RequestResponse> CreateNewExchangeAccount(ExchangeAccount exchangeAccount, int appUserId)
        {
            // FIX #3: Removed plaintext Username/Password copy.
            // Credentials should be stored via UserCredential with per-user encryption.
            ExchangeAccount newExchangeAccount = new ExchangeAccount
            {
                Name = exchangeAccount.Name,
                Description = exchangeAccount.Description,
                Notes = exchangeAccount.Notes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AppUserId = appUserId,  // FIX F-02: Set owner on creation
            };

            try
            {
                await _db.AddAsync(newExchangeAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(newExchangeAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exchange account for user {UserId}", appUserId);
                return new RequestResponse(null, 500, "There was an error creating the exchange account.");
            }
        }

        private async Task<RequestResponse> UpdateExistingExchangeAccount(ExchangeAccount exchangeAccount, int appUserId)
        {
            try
            {
                // FIX F-02: Verify ownership before update
                ExchangeAccount existingExchangeAccount = await _db.ExchangeAccounts
                    .Where(ea => ea.Id == exchangeAccount.Id && ea.AppUserId == appUserId)
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
                existingExchangeAccount.LastModifiedAt = DateTime.UtcNow;

                _db.Update(existingExchangeAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(existingExchangeAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exchange account {Id} for user {UserId}", exchangeAccount.Id, appUserId);
                return new RequestResponse(null, 500, "There was an error updating the exchange account.");
            }
        }
    }
}
