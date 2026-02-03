#nullable disable
namespace egibi_api.Data.Entities
{
    public class ExchangeFeeStructure : EntityBase
    {
        public bool? IsRolling { get; set; }
        public int? RollingIntervalDays { get; set; }
        public DateTime? RollingReset { get; set; }
       
        //public int? ExchangeId { get; set; }
        //public virtual Exchange Exchange { get; set; }

        public ICollection<ExchangeFeeStructureTier> ExchangeFeeStructureTiers { get; } = new List<ExchangeFeeStructureTier>();
        //public List<ExchangeFeeStructureTier> ExchangeFeeStructureTiers { get; set; } = new();
    }
}
