namespace egibi_api.Data.Entities
{
    public class AccountApi : EntityBase
    {
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
