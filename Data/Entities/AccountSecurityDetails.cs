#nullable disable
namespace egibi_api.Data.Entities
{
    public class AccountSecurityDetails : EntityBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool TwoFactorEnabled {get;set;}
    }
}
