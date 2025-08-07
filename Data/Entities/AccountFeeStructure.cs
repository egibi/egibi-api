namespace egibi_api.Data.Entities
{
    public class AccountFeeStructure : EntityBase
    {
        public int? AccountId { get; set; }
        public virtual Account Account { get; set; }
    }
}
