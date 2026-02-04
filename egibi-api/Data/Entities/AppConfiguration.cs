#nullable disable
namespace egibi_api.Data.Entities
{
    /// <summary>
    /// General-purpose key-value configuration store.
    /// Uses EntityBase.Name as the config key and EntityBase.Description as the JSON value.
    /// Used by StorageService to persist storage/archival settings.
    /// </summary>
    public class AppConfiguration : EntityBase
    {
    }
}