#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;
using Account = egibi_api.Data.Entities.Account;

namespace egibi_api.Services
{
    public class AccountsService
    {
        private readonly EgibiDbContext _db;
        public AccountsService(EgibiDbContext db)
        {
            _db = db;
        }

        public async Task<RequestResponse> GetAccounts()
        {
            try
            {
                List<Account> accounts = await _db.Accounts
                .ToListAsync();

                return new RequestResponse(accounts, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetAccount(int id)
        {
            try
            {
                var account = await _db.Accounts
                    .FirstOrDefaultAsync();

                return new RequestResponse(account, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetAccountTypes()
        {
            try
            {
                var accountTypes = await _db.AccountTypes
                    .ToListAsync();

                return new RequestResponse(accountTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteAccount(int id)
        {
            try
            {
                _db.Remove(_db.Accounts
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

        public async Task<RequestResponse> DeleteAccounts(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.Accounts
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveAccount(Account account)
        {
            try
            {
                if (account.Id == 0)
                    return await CreateNewAccount(account);
                else
                    return await UpdateExistingAccount(account);


            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> CreateNewAccount(Account account)
        {
            Account newAccount = new Account
            {
                Name = account.Name,
                //AccountTypeId = account.AccountType.Id,
                Url = account.Url,
                Description = account.Description,
                Notes = account.Notes,
                IsActive = true,
                CreatedAt = DateTime.Now.ToUniversalTime()
            };

            try
            {
                await _db.AddAsync(newAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(newAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> UpdateExistingAccount(Account account)
        {
            try
            {
                Account existingAccount = await _db.Accounts
                    .Where(w => w.Id == account.Id)
                    .FirstOrDefaultAsync();

                existingAccount.Name = account.Name;
                existingAccount.Url = account.Url;
               // existingAccount.AccountTypeId = account.AccountType.Id;
                existingAccount.Description = account.Description;
                existingAccount.Notes = account.Notes;
                existingAccount.IsActive = account.IsActive;
                existingAccount.LastModifiedAt = DateTime.Now.ToUniversalTime();

                _db.Update(existingAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(account, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        // DETAILS
        public async Task<RequestResponse> GetAccountDetails(int id)
        {
            try
            {
                var accountDetails = await _db.AccountDetails
                    .Include("AccountType")
                    .Where(w => w.AccountId == id)
                    .FirstOrDefaultAsync();

                return new RequestResponse(accountDetails, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        //public async Task<RequestResponse> SaveAccountDetails(AccountDetails accountDetails)
        //{
        //    try
        //    {
        //        var account = await _db.Accounts
        //            .Where(w => w.Id == accountDetails.AccountId)
        //            .Select(s => s.AccountDetails)
        //            .FirstOrDefaultAsync();

        //        var existingAccountDetails = account.AccountDetails;

        //    }
        //    catch(Exception ex)
        //    {
        //        return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
        //    }

        //}
    }
}
