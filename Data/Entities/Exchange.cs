#nullable disable
namespace egibi_api.Data.Entities
{
    public class Exchange : EntityBase
    {
        public int? ExchangeFeeStructureId { get; set; }
        public virtual ExchangeFeeStructureTier ExchangeFeeStructureTier { get; set; }
    }
}
