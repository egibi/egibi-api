#nullable disable

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Represents an external service/exchange in the service catalog.
    /// Each Connection is a "template" — users create Accounts linked to a Connection,
    /// with their own encrypted credentials stored in UserCredential.
    ///
    /// Legacy fields (ApiKey, ApiSecretKey) are retained for backward compatibility
    /// but should not be used for new accounts — use UserCredential instead.
    /// </summary>
    public class Connection : EntityBase
    {
        // =============================================
        // LEGACY FIELDS (deprecated — use UserCredential)
        // =============================================
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecretKey { get; set; }
        public bool? IsDataSource { get; set; }

        public int? ConnectionTypeId { get; set; }
        public virtual ConnectionType ConnectionType { get; set; }

        // =============================================
        // SERVICE CATALOG FIELDS
        // =============================================

        /// <summary>
        /// Service category for grouping in the UI card picker.
        /// Values: "crypto_exchange", "stock_broker", "data_provider", "other"
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Icon key used by the frontend to render the service's SVG icon.
        /// e.g., "binance", "coinbase", "schwab", "alpaca", "kraken"
        /// </summary>
        public string IconKey { get; set; }

        /// <summary>
        /// Brand color hex code for the service card.
        /// e.g., "#F0B90B" for Binance, "#0052FF" for Coinbase
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Service website URL for reference / external link.
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// Default base URL for API connections, pre-filled during account setup.
        /// Users can override this per-account if needed.
        /// </summary>
        public string DefaultBaseUrl { get; set; }

        /// <summary>
        /// JSON array of credential field identifiers required by this service.
        /// e.g., ["api_key","api_secret"] or ["api_key","api_secret","passphrase"]
        /// The frontend uses this to dynamically render the credential form.
        /// 
        /// Available field keys:
        ///   api_key, api_secret, passphrase, username, password, base_url
        /// </summary>
        public string RequiredFields { get; set; }

        /// <summary>
        /// Display order for sorting in the service catalog and card picker.
        /// Lower numbers appear first.
        /// </summary>
        public int SortOrder { get; set; }
    }
}
