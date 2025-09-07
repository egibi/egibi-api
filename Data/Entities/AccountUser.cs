#nullable disable
namespace egibi_api.Data.Entities
{
    public class AccountUser : EntityBase
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }

        public int? AccountUserId { get; set; }
        public virtual ICollection<Account> Accounts { get; set; }

    }
}
