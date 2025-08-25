#nullable disable
namespace egibi_api.Data.Entities
{
    public class Account : EntityBase
    {
        public int? AccountTypeId { get; set; }
        public virtual AccountType AccountType { get; set; }

        public int? AccountDetailsId { get; set; }
        public virtual AccountDetails AccountDetails { get; set; }

        //public int? AccountApiDetailsId { get; set; }
        //public virtual AccountApiDetails AccountApiDetails { get; set; }

        //public int? AccountFeeStructureDetailsId {get;set;}
        //public virtual AccountFeeStructureDetails AccountFeeStructureDetails { get; set; }

        //public int? AccountSecurityDetailsId { get; set; }
        //public virtual AccountSecurityDetails AccountSecurityDetails { get; set; }

        //public int? AccountStatusDetailsId { get; set; }
        //public AccountStatusDetails AccountStatusDetails { get; set; }


    }
}
