#nullable disable
namespace egibi_api.Data.Entities
{
    public class AccountStatus : EntityBase
    {
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
