#nullable disable
namespace egibi_api.Data.Entities
{
    public class AccountStatusDetails : EntityBase
    {
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
