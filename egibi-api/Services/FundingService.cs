#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Models;
using egibi_api.Services.Security;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace egibi_api.Services
{
    /// <summary>
    /// Manages funding source operations — provider catalog, Mercury (API-key) setup,
    /// and primary funding source lifecycle.
    /// Plaid-linked sources are created via PlaidService.ExchangePublicToken instead.
    /// </summary>
    public class FundingService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<FundingService> _logger;

        public FundingService(EgibiDbContext db, IEncryptionService encryption, ILogger<FundingService> logger)
        {
            _db = db;
            _encryption = encryption;
            _logger = logger;
        }

        // =============================================
        // GET FUNDING PROVIDERS
        // =============================================

        /// <summary>
        /// Returns available funding providers for the setup wizard picker.
        /// Only active connections with category "funding_provider" are returned.
        /// </summary>
        public async Task<RequestResponse> GetFundingProviders()
        {
            try
            {
                var providers = await _db.Connections
                    .Where(c => c.Category == "funding_provider" && c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new FundingProviderEntry
                    {
                        ConnectionId = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IconKey = c.IconKey,
                        Color = c.Color,
                        Website = c.Website,
                        DefaultBaseUrl = c.DefaultBaseUrl,
                        RequiredFields = ParseRequiredFields(c.RequiredFields),
                        SignupUrl = c.SignupUrl,
                        ApiDocsUrl = c.ApiDocsUrl,
                        LinkMethod = c.LinkMethod
                    })
                    .ToListAsync();

                return new RequestResponse(providers, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFundingProviders failed");
                return new RequestResponse(null, 500, "Failed to load funding providers", new ResponseError(ex));
            }
        }

        // =============================================
        // GET PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Returns the user's primary funding source with masked credentials.
        /// Returns 200 with null data if no primary funding source exists.
        /// </summary>
        public async Task<RequestResponse> GetPrimaryFundingSource(int userId)
        {
            try
            {
                var fs = await _db.FundingSources
                    .Include(f => f.Connection)
                    .FirstOrDefaultAsync(f => f.AppUserId == userId && f.IsPrimary && f.IsActive);

                if (fs == null)
                    return new RequestResponse(null, 200, "No primary funding source configured");

                // Build credential summary for API-key sources
                string maskedApiKey = null;
                bool hasCredentials = false;
                string credentialLabel = null;
                DateTime? lastUsedAt = null;

                if (fs.LinkMethod == "api_key" && fs.ConnectionId > 0)
                {
                    var credential = await _db.UserCredentials
                        .FirstOrDefaultAsync(uc => uc.AppUserId == userId && uc.ConnectionId == fs.ConnectionId);

                    if (credential != null)
                    {
                        hasCredentials = !string.IsNullOrEmpty(credential.EncryptedApiKey);
                        credentialLabel = credential.Label;
                        lastUsedAt = credential.LastUsedAt;

                        if (hasCredentials)
                        {
                            try
                            {
                                var appUser = await _db.AppUsers.FindAsync(userId);
                                var plainKey = _encryption.Decrypt(credential.EncryptedApiKey, appUser.EncryptedDataKey);
                                maskedApiKey = MaskApiKey(plainKey);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to decrypt API key for masking, user {UserId}", userId);
                                maskedApiKey = "••••••••";
                                hasCredentials = true;
                            }
                        }
                    }
                }

                // Build Plaid details if applicable
                PlaidFundingDetails plaidDetails = null;
                if (fs.LinkMethod == "plaid_link")
                {
                    hasCredentials = !string.IsNullOrEmpty(fs.EncryptedPlaidAccessToken);
                    plaidDetails = new PlaidFundingDetails
                    {
                        InstitutionName = fs.Description,
                        PlaidAccountId = fs.PlaidAccountId,
                        AccountName = fs.PlaidAccountName,
                        Mask = fs.PlaidAccountMask,
                        LastSyncedAt = fs.LastModifiedAt
                    };
                }

                var response = new FundingSourceResponse
                {
                    AccountId = fs.Id,
                    AccountName = fs.Name,
                    Description = fs.Description,
                    IsActive = fs.IsActive,
                    CreatedAt = fs.CreatedAt,

                    ConnectionId = fs.ConnectionId,
                    ProviderName = fs.Connection?.Name,
                    ProviderIconKey = fs.Connection?.IconKey,
                    ProviderColor = fs.Connection?.Color,
                    ProviderWebsite = fs.Connection?.Website,
                    BaseUrl = fs.BaseUrl ?? fs.Connection?.DefaultBaseUrl,

                    HasCredentials = hasCredentials,
                    CredentialLabel = credentialLabel,
                    MaskedApiKey = maskedApiKey,
                    CredentialLastUsedAt = lastUsedAt,

                    LinkMethod = fs.LinkMethod,
                    PlaidDetails = plaidDetails
                };

                return new RequestResponse(response, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPrimaryFundingSource failed for user {UserId}", userId);
                return new RequestResponse(null, 500, "Failed to load funding source", new ResponseError(ex));
            }
        }

        // =============================================
        // SET PRIMARY FUNDING SOURCE (API-KEY FLOW)
        // =============================================

        /// <summary>
        /// Creates a new funding source as the user's primary (for API-key providers like Mercury).
        /// Encrypts credentials and demotes any existing primary.
        /// </summary>
        public async Task<RequestResponse> SetPrimaryFundingSource(CreateFundingSourceRequest request, int userId)
        {
            try
            {
                // Validate connection exists and is a funding provider
                var connection = await _db.Connections
                    .FirstOrDefaultAsync(c => c.Id == request.ConnectionId && c.Category == "funding_provider" && c.IsActive);

                if (connection == null)
                    return new RequestResponse(null, 404, "Funding provider not found");

                // Get user for encryption
                var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                if (appUser == null)
                    return new RequestResponse(null, 404, "User not found");

                if (string.IsNullOrEmpty(appUser.EncryptedDataKey))
                    return new RequestResponse(null, 400, "User encryption key not configured");

                // Demote existing primary
                var existingPrimary = await _db.FundingSources
                    .FirstOrDefaultAsync(f => f.AppUserId == userId && f.IsPrimary);

                if (existingPrimary != null)
                {
                    existingPrimary.IsPrimary = false;
                    existingPrimary.LastModifiedAt = DateTime.UtcNow;
                }

                // Create the funding source
                var fundingSource = new FundingSource
                {
                    Name = request.Name,
                    Description = request.Description ?? connection.Description,
                    AppUserId = userId,
                    ConnectionId = request.ConnectionId,
                    IsPrimary = true,
                    LinkMethod = connection.LinkMethod ?? "api_key",
                    BaseUrl = request.BaseUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _db.FundingSources.AddAsync(fundingSource);

                // Encrypt and store credentials (for api_key providers)
                if (request.Credentials != null && connection.LinkMethod == "api_key")
                {
                    // Find or create UserCredential for this connection
                    var credential = await _db.UserCredentials
                        .FirstOrDefaultAsync(uc => uc.AppUserId == userId && uc.ConnectionId == request.ConnectionId);

                    if (credential == null)
                    {
                        credential = new UserCredential
                        {
                            AppUserId = userId,
                            ConnectionId = request.ConnectionId,
                            Name = $"{connection.Name} credentials",
                            Label = $"{request.Name} API Key",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            KeyVersion = 1
                        };
                        await _db.UserCredentials.AddAsync(credential);
                    }

                    if (!string.IsNullOrEmpty(request.Credentials.ApiKey))
                        credential.EncryptedApiKey = _encryption.Encrypt(request.Credentials.ApiKey, appUser.EncryptedDataKey);

                    if (!string.IsNullOrEmpty(request.Credentials.ApiSecret))
                        credential.EncryptedApiSecret = _encryption.Encrypt(request.Credentials.ApiSecret, appUser.EncryptedDataKey);

                    credential.LastModifiedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Primary funding source set for user {UserId}: {Provider}", userId, connection.Name);

                return new RequestResponse(
                    new { fundingSourceId = fundingSource.Id, providerName = connection.Name },
                    200, "Primary funding source configured");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetPrimaryFundingSource failed for user {UserId}", userId);
                return new RequestResponse(null, 500, "Failed to set funding source", new ResponseError(ex));
            }
        }

        // =============================================
        // REMOVE PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Removes the primary funding flag from the user's current primary.
        /// The funding source record is kept but no longer marked as primary.
        /// </summary>
        public async Task<RequestResponse> RemovePrimaryFundingSource(int userId)
        {
            try
            {
                var primary = await _db.FundingSources
                    .FirstOrDefaultAsync(f => f.AppUserId == userId && f.IsPrimary);

                if (primary == null)
                    return new RequestResponse(null, 404, "No primary funding source found");

                primary.IsPrimary = false;
                primary.LastModifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Primary funding source removed for user {UserId}", userId);
                return new RequestResponse(primary.Id, 200, "Primary funding source removed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemovePrimaryFundingSource failed for user {UserId}", userId);
                return new RequestResponse(null, 500, "Failed to remove funding source", new ResponseError(ex));
            }
        }

        // =============================================
        // PRIVATE HELPERS
        // =============================================

        /// <summary>
        /// Masks an API key for display: shows first 4 and last 4 chars.
        /// </summary>
        private static string MaskApiKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.Length <= 8) return "••••••••";
            return $"{key[..4]}••••{key[^4..]}";
        }

        /// <summary>
        /// Parses the RequiredFields JSON array from the Connection entity.
        /// Returns empty array on null or malformed JSON.
        /// </summary>
        private static string[] ParseRequiredFields(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
            try { return JsonSerializer.Deserialize<string[]>(json); }
            catch { return Array.Empty<string>(); }
        }
    }
}
