#nullable disable
namespace egibi_api.Configuration
{
    /// <summary>
    /// Configuration options for the Plaid API integration.
    /// Bound from appsettings "Plaid" section.
    /// </summary>
    public class PlaidOptions
    {
        public string ClientId { get; set; }
        public string Secret { get; set; }

        /// <summary>
        /// Plaid environment: "sandbox", "development", or "production".
        /// Determines the base URL for API calls.
        /// </summary>
        public string Environment { get; set; } = "sandbox";

        /// <summary>
        /// Resolved base URL based on Environment.
        /// </summary>
        public string BaseUrl => Environment?.ToLower() switch
        {
            "production" => "https://production.plaid.com",
            "development" => "https://development.plaid.com",
            _ => "https://sandbox.plaid.com"
        };

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
