#nullable disable
namespace egibi_api.Data.Entities
{
    public class ExchangeAccount : EntityBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public decimal CurrentSpotVolume_30Day { get; set; }
        public decimal AssetBalance { get; set; }

        public int? ExchangeId { get; set; }
        public virtual Exchange Exchange { get; set; }

        public int? ExchangeFeeStructureTierId { get; set; }
        public ExchangeFeeStructureTier ExchangeFeeStructureTier { get; set; }
    }
}
