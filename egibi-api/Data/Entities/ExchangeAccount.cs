#nullable disable
namespace egibi_api.Data.Entities
{
    public class ExchangeAccount : EntityBase
    {
        // FIX #3: Removed plaintext Username and Password properties.
        // Credentials should be stored via UserCredential with per-user AES-256-GCM encryption.

        public decimal CurrentSpotVolume_30Day { get; set; }
        public decimal AssetBalance { get; set; }

        public int? ExchangeId { get; set; }
        public virtual Exchange Exchange { get; set; }

        public int? ExchangeFeeStructureTierId { get; set; }
        public ExchangeFeeStructureTier ExchangeFeeStructureTier { get; set; }

        // FIX F-02: Add user ownership for data scoping
        /// <summary>
        /// The authenticated user who owns this exchange account.
        /// All queries must be scoped by this field to prevent IDOR.
        /// </summary>
        public int? AppUserId { get; set; }
        public virtual AppUser AppUser { get; set; }
    }
}
