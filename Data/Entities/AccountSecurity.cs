#nullable disable
namespace egibi_api.Data.Entities
{
    public class AccountSecurity : EntityBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool TwoFactorEnabled {get;set;}

        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
