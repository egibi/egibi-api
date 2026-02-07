#nullable disable
namespace egibi_api.Data.Entities
{
    public class Account : EntityBase
    {
        public bool IsNewAccount { get; set; }

        public int? AccountTypeId { get; set; }
        public virtual AccountType AccountType { get; set; }

        public int? AccountUserId { get; set; }
        public virtual AccountUser AccountUser { get; set; }

        public int? AccountDetailsId { get; set; }
        public virtual AccountDetails AccountDetails { get; set; }

        // =============================================
        // SERVICE LINK
        // =============================================

        /// <summary>
        /// The service/exchange this account is connected to.
        /// Null for "custom" accounts with no predefined service.
        /// </summary>
        public int? ConnectionId { get; set; }
        public virtual Connection Connection { get; set; }

        /// <summary>
        /// The authenticated user who owns this account.
        /// Links to AppUser (OIDC-authenticated user) rather than legacy AccountUser.
        /// </summary>
        public int? AppUserId { get; set; }
        public virtual AppUser AppUser { get; set; }

        /// <summary>
        /// Marks this account as the user's primary funding source.
        /// Only one account per user should have this set to true.
        /// </summary>
        public bool IsPrimaryFunding { get; set; }

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
