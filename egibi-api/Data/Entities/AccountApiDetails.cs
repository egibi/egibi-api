#nullable disable
namespace egibi_api.Data.Entities
{
    public class AccountApiDetails : EntityBase
    {
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
