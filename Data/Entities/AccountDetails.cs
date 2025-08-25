#nullable disable
namespace egibi_api.Data.Entities
{
    public class AccountDetails : EntityBase
    {
        public string User { get; set; }
        public string Url { get; set; }

        public int? AccountId { get; set; }
        public virtual Account Account {get;set;}
    
    }
}
