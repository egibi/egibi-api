#nullable disable
namespace egibi_api.Data.Entities
{
    public class Account : EntityBase
    {
        public string Url { get; set; }
        public int? AccountTypeId { get; set; }
        public virtual AccountType AccountType {get;set;}

    }
}
