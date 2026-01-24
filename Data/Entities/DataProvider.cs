#nullable disable
namespace egibi_api.Data.Entities
{
    public class DataProvider : EntityBase
    {
        public bool IsLive { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }


        public int? DataProviderTypeId { get; set; }
        public virtual DataProviderType DataProviderType { get; set; }

        public int? DataFormatTypeId { get; set; }
        public virtual DataFormatType DataFormatType { get; set; }

        public int? DataFrequencyTypeId { get; set; }
        public virtual DataFrequencyType DataFrequencyType { get; set; }
    }
}