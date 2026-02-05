#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Stores fee structure details for a specific account.
    /// Each account can have one fee structure that defines maker/taker fees.
    /// </summary>
    public class AccountFeeStructureDetails : EntityBase
    {
        /// <summary>
        /// Maker fee percentage (e.g., 0.10 for 0.10%).
        /// </summary>
        [Column(TypeName = "decimal(8,4)")]
        public decimal MakerFeePercent { get; set; }

        /// <summary>
        /// Taker fee percentage (e.g., 0.15 for 0.15%).
        /// </summary>
        [Column(TypeName = "decimal(8,4)")]
        public decimal TakerFeePercent { get; set; }

        /// <summary>
        /// Fee schedule type: "flat", "tiered", or "volume".
        /// Determines how fees are calculated.
        /// </summary>
        [MaxLength(50)]
        public string FeeScheduleType { get; set; } = "flat";

        /// <summary>
        /// The account this fee structure belongs to.
        /// </summary>
        [Required]
        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public virtual Account Account { get; set; }
    }
}
