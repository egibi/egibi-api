#nullable disable
namespace egibi_api.Data.Entities
{
    public class ExchangeFeeStructureTier : EntityBase
    {
        public int TierLevel { get; set; }
        public decimal MakerFeeSpot { get; set; }
        public decimal TakerFeeSpot { get; set; }
        public decimal MakerFeeFuture { get; set; }
        public decimal TakerFeeFuture { get; set; }
        public decimal? SpotValue { get; set; }
        public decimal? AssetBalance { get; set; }

        public int? ExchangeFeeStructureId { get; set; }
        public virtual ExchangeFeeStructure ExchangeFeeStructure { get; set; }
    }
}
