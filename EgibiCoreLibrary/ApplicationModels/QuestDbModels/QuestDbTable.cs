#nullable disable
namespace EgibiCoreLibrary.Models.QuestDbModels
{
    public class QuestDbTable
    {
        public QuestDbTable()
        {
            TableColumns = new List<QuestDbTableColumn>();
        }

        public string TableName { get; set; }
        public string TablePartitionBy { get; set; }
        public List<QuestDbTableColumn> TableColumns { get; set; }

    }

    public class QuestDbTableColumn
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
    }
}
