#nullable disable
namespace egibi_api.Configuration
{
    /// <summary>
    /// App-level Plaid configuration defaults.
    /// User-specific credentials (ClientId, Secret, Environment) are stored
    /// per-user in the UserPlaidConfig table, encrypted with each user's DEK.
    /// </summary>
    public class PlaidOptions
    {
        /// <summary>
        /// Plaid products to request during Link (e.g., "auth", "transactions").
        /// </summary>
        public string[] Products { get; set; } = new[] { "auth", "transactions" };

        /// <summary>
        /// Country codes for Plaid Link (default: US).
        /// </summary>
        public string[] CountryCodes { get; set; } = new[] { "US" };

        /// <summary>
        /// Optional webhook URL for Plaid to send notifications.
        /// </summary>
        public string WebhookUrl { get; set; }

        /// <summary>
        /// Optional redirect URI for OAuth-based institutions.
        /// </summary>
        public string RedirectUri { get; set; }
    }
}
