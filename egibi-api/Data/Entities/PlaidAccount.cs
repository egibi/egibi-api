#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Represents an individual bank account linked through Plaid.
    /// Each PlaidItem (linked bank) can have multiple accounts
    /// (e.g., Checking, Savings, Credit Card).
    /// </summary>
    public class PlaidAccount : EntityBase
    {
        // =============================================
        // RELATIONSHIPS
        // =============================================

        [Required]
        public int PlaidItemId { get; set; }

        [ForeignKey(nameof(PlaidItemId))]
        public virtual PlaidItem PlaidItem { get; set; }

        // =============================================
        // PLAID ACCOUNT FIELDS
        // =============================================

        /// <summary>
        /// Plaid account_id — unique identifier for this account within the item.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string PlaidAccountId { get; set; }

        /// <summary>
        /// Account display name from the institution (e.g., "Plaid Checking").
        /// </summary>
        [MaxLength(300)]
        public string OfficialName { get; set; }

        /// <summary>
        /// Last 4 digits of the account number (e.g., "0000", "1234").
        /// </summary>
        [MaxLength(10)]
        public string Mask { get; set; }

        /// <summary>
        /// Account type: depository, credit, loan, investment, brokerage, other.
        /// </summary>
        [MaxLength(50)]
        public string AccountType { get; set; }

        /// <summary>
        /// Account subtype: checking, savings, credit card, etc.
        /// </summary>
        [MaxLength(50)]
        public string AccountSubtype { get; set; }

        /// <summary>
        /// Whether this specific account is selected as the primary funding account.
        /// Only one PlaidAccount per user should be the selected funding account.
        /// </summary>
        public bool IsSelectedFunding { get; set; }

        // =============================================
        // BALANCE CACHE
        // =============================================

        /// <summary>
        /// Last known available balance (funds available for spending/withdrawal).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AvailableBalance { get; set; }

        /// <summary>
        /// Last known current balance (total balance including pending).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CurrentBalance { get; set; }

        /// <summary>
        /// ISO currency code (e.g., "USD").
        /// </summary>
        [MaxLength(10)]
        public string IsoCurrencyCode { get; set; }

        /// <summary>
        /// When balances were last refreshed from Plaid.
        /// </summary>
        public DateTime? BalanceLastUpdatedAt { get; set; }
    }
}
