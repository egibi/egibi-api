using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Account = egibi_api.Data.Entities.Account;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountsController
    {
        private readonly AccountsService _accountsService;

        public AccountsController(AccountsService accountsService)
        {
            _accountsService = accountsService;
        }

        [HttpGet("get-accounts")]
        public async Task<RequestResponse> GetAccounts()
        {
            return await _accountsService.GetAccounts();
        }

        [HttpGet("get-account")]
        public async Task<RequestResponse> GetAccount(int id)
        {
            return await _accountsService.GetAccount(id);
        }

        [HttpGet("get-account-types")]
        public async Task<RequestResponse> GetAccountTypes()
        {
            return await _accountsService.GetAccountTypes();
        }

        [HttpPost("save-account")]
        public async Task<RequestResponse> SaveAccount(Account account)
        {
            return await _accountsService.SaveAccount(account);
        }

        [HttpDelete("delete-accounts")]
        public async Task<RequestResponse> DeleteAccounts(List<int> ids)
        {
            return await _accountsService.DeleteAccounts(ids);
        }

        [HttpDelete("delete-account")]
        public async Task<RequestResponse> DeleteAccount(int id)
        {
            return await _accountsService.DeleteAccount(id);
        }
    }
}
