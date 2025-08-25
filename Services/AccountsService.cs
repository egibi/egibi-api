#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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

        public async Task<RequestResponse> SaveAccount(Account account, bool newAccount, AccountDetails accountDetails = null)
        {
            try
            {
                if (account.Id == 0 || newAccount)

                    if (accountDetails != null)
                    {
                        return await CreateNewAccount(account, accountDetails);
                    }
                    else
                    {
                        return await CreateNewAccount(account);
                    }


                    //return await CreateNewAccount(account, AccountDetails accountDetails);
                    else
                        return await UpdateExistingAccount(account);


            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> CreateNewAccount(Account account, AccountDetails accountDetails = null)
        {
            Account newAccount = new Account
            {
                Name = account.Name,
                //AccountTypeId = account.AccountType.Id,
                Description = account.Description,
                Notes = account.Notes,
                IsActive = true,
                CreatedAt = DateTime.Now.ToUniversalTime(),
                AccountDetails = accountDetails
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

        public async Task<RequestResponse> SaveAccountDetails(AccountDetails accountDetails)
        {
            try
            {
                var account = await _db.Accounts
                    .FirstOrDefaultAsync(x => x.Id == accountDetails.AccountId);

                if (account != null)
                {
                    // add details to existing account
                }
                else
                {
                    // create new account and apply details
                    Account newAccount = new Account
                    {
                        AccountDetails = accountDetails,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        Description = "New Account",
                        IsActive = true,
                        Name = accountDetails.Name
                    };

                    await CreateNewAccount(newAccount, accountDetails);
                }

                await _db.SaveChangesAsync();
                return new RequestResponse(accountDetails, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }


        }

        //public async Task<RequestResponse> SaveAccountDetails(AccountDetails accountDetails)
        //{
        //    Account existingAccount = null;

        //    try
        //    {
        //        existingAccount = await _db.Accounts
        //           .Where(w => w.Id == accountDetails.AccountId)
        //           .FirstOrDefaultAsync();

        //        if (existingAccount == null)
        //        {
        //            // this is a new account, create new Account entity and apply AccountDetails
        //            Account newAccount = new Account
        //            {
        //                Name = accountDetails.Name,
        //                Description = "New account",
        //                IsActive = true,
        //                CreatedAt = DateTime.Now.ToUniversalTime()
        //            };

        //            await _db.AddAsync(newAccount);
        //            await _db.SaveChangesAsync();

        //            int newAccountId = newAccount.Id;
        //            var recentlyCreatedAccount = await _db.Accounts
        //                .FirstOrDefaultAsync(x => x.Id == newAccountId);

        //            recentlyCreatedAccount.AccountDetails = accountDetails;
        //            await _db.SaveChangesAsync();

        //            return new RequestResponse(accountDetails, 200, "OK");
        //        }
        //        else
        //        {
        //            if(existingAccount != null)
        //            {
        //                AccountDetails existingAccountDetails = await _db.AccountDetails
        //                    .Where(w => w.AccountId == existingAccount.Id)
        //                    .FirstOrDefaultAsync();

        //                if(existingAccountDetails != null)
        //                {
        //                    existingAccount.AccountDetails = accountDetails;
        //                    await _db.SaveChangesAsync();
        //                }
        //            }

        //            return new RequestResponse(accountDetails, 200, "OK");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
        //    }
        //}
    }
}
