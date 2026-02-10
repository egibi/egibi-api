#nullable disable
using egibi_api.Configuration;
using egibi_api.Data;
using egibi_api.Hubs;
using egibi_api.MarketData.Fetchers;
using egibi_api.MarketData.Repositories;
using egibi_api.MarketData.Services;
using egibi_api.Services;
using egibi_api.Services.Security;
using EgibiBinanceUsSdk;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenIddict.Abstractions;
using OtpNet;

namespace egibi_api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var CorsPolicyDev = "_corsPolicyDev";
            var CorsPolicyProd = "_corsPolicyProd";
            var builder = WebApplication.CreateBuilder(args);
            string env = builder.Environment.EnvironmentName;
            string dbConnectionString = string.Empty;
            string questDbConnectionString = string.Empty;

            // QuestDB options binding
            builder.Services.Configure<QuestDbOptions>(
                builder.Configuration.GetSection(QuestDbOptions.SectionName));

            // Plaid options binding
            builder.Services.Configure<PlaidOptions>(
                builder.Configuration.GetSection("Plaid"));

            Console.WriteLine($"env={builder.Environment.EnvironmentName}");

            // Auto-start Docker databases in Development
            if (builder.Environment.IsDevelopment())
            {
                await DevInfrastructure.EnsureDatabasesAsync(builder.Environment.ContentRootPath);
            }

            if (builder.Environment.IsProduction())
            {
                dbConnectionString = builder.Configuration.GetConnectionString("EgibiDb");
                questDbConnectionString = builder.Configuration.GetConnectionString("QuestDb");

                builder.Services.AddDbContextPool<EgibiDbContext>(options =>
                {
                    options.UseNpgsql(dbConnectionString);
                    options.UseOpenIddict();
                });

                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "https://www.egibi.io" };

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy(name: CorsPolicyProd,
                        policy =>
                        {
                            policy.WithOrigins(allowedOrigins)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });
                });
            }

            if (builder.Environment.IsDevelopment())
            {
                dbConnectionString = builder.Configuration.GetConnectionString("EgibiDb");
                questDbConnectionString = builder.Configuration.GetConnectionString("QuestDb");

                Console.WriteLine($"ConnectionString={dbConnectionString}");

                builder.Services.AddDbContext<EgibiDbContext>(options =>
                {
                    options.UseNpgsql(dbConnectionString);

                    // Register OpenIddict entity sets in EF Core
                    options.UseOpenIddict();
                });

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy(name: CorsPolicyDev,
                        policy =>
                        {
                            policy.WithOrigins(
                                    "http://localhost:4200",
                                    "https://localhost:4200")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials(); // Required for cookie auth in OIDC flow
                        });
                });
            }

            // Add services to the container.
            builder.Services.AddHttpClient<BinanceUsHttpClient>();
            builder.Services.AddScoped<ApiTesterService>();
            builder.Services.AddScoped<ConnectionsService>();
            builder.Services.AddScoped<DataManagerService>();
            builder.Services.AddScoped<BacktesterService>();
            builder.Services.AddScoped<egibi_api.Services.Backtesting.BacktestExecutionService>();
            builder.Services.AddScoped<MarketsService>();
            builder.Services.AddScoped<AppConfigurationsService>();
            builder.Services.AddScoped<AccountsService>();
            builder.Services.AddScoped<FundingService>();
            builder.Services.AddHttpClient<PlaidApiClient>();
            builder.Services.AddScoped<PlaidService>();
            builder.Services.AddScoped(service => new QuestDbService(questDbConnectionString));
            builder.Services.AddScoped<TestingService>();
            builder.Services.AddScoped<GeoDateTimeDataService>();
            builder.Services.AddScoped<StorageService>();
            builder.Services.AddScoped<MfaService>();
            builder.Services.AddHttpClient();

            // FIX #13: Register previously missing services
            builder.Services.AddScoped<ExchangeAccountsService>();
            builder.Services.AddScoped<ExchangesService>();

            // --- Security & Auth ---
            // TEMP DIAGNOSTIC — remove after confirming MasterKey loads
            var rawMasterKey = builder.Configuration["Encryption:MasterKey"];
            Console.WriteLine($"[DIAG] Encryption:MasterKey is null: {rawMasterKey == null}");
            Console.WriteLine($"[DIAG] Encryption:MasterKey is empty: {string.IsNullOrWhiteSpace(rawMasterKey)}");
            Console.WriteLine($"[DIAG] Encryption:MasterKey length: {rawMasterKey?.Length ?? 0}");
            Console.WriteLine($"[DIAG] ENV Encryption__MasterKey is set: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Encryption__MasterKey"))}");
            Console.WriteLine($"[DIAG] ENV Encryption__MasterKey length: {Environment.GetEnvironmentVariable("Encryption__MasterKey")?.Length ?? 0}");
            Console.WriteLine($"[DIAG] ASPNETCORE_ENVIRONMENT: {builder.Environment.EnvironmentName}");
            // END TEMP DIAGNOSTIC

            var masterKey = rawMasterKey;
            builder.Services.AddSingleton<IEncryptionService>(new EncryptionService(masterKey));
            builder.Services.AddScoped<AppUserService>();

            // --- OpenIddict OIDC Server ---
            builder.Services.AddOpenIddict()
                .AddCore(options =>
                {
                    // Use EF Core stores for tokens, applications, authorizations, scopes
                    options.UseEntityFrameworkCore()
                        .UseDbContext<EgibiDbContext>();
                })
                .AddServer(options =>
                {
                    // Enable the standard OIDC endpoints
                    options.SetAuthorizationEndpointUris("connect/authorize")
                           .SetTokenEndpointUris("connect/token")
                           .SetUserinfoEndpointUris("connect/userinfo")
                           .SetLogoutEndpointUris("connect/logout");

                    // Enable the flows we need
                    // Authorization Code + PKCE for the Angular SPA
                    // Client Credentials for future service-to-service
                    // Refresh Token for session persistence
                    options.AllowAuthorizationCodeFlow()
                           .AllowClientCredentialsFlow()
                           .AllowRefreshTokenFlow();

                    // Require PKCE for authorization code flow (security best practice for SPAs)
                    options.RequireProofKeyForCodeExchange();

                    // Token lifetimes
                    options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
                    options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));

                    // Register scopes (including offline_access for refresh tokens)
                    options.RegisterScopes(
                        OpenIddictConstants.Scopes.OpenId,
                        OpenIddictConstants.Scopes.Email,
                        OpenIddictConstants.Scopes.Profile,
                        OpenIddictConstants.Scopes.Roles,
                        OpenIddictConstants.Scopes.OfflineAccess,
                        "api");

                    // Use ASP.NET Core Data Protection for token format (development-friendly)
                    options.UseDataProtection();

                    // Signing & encryption certificates
                    if (builder.Environment.IsProduction())
                    {
                        var signingCertBase64 = builder.Configuration["OIDC_SIGNING_CERT"];
                        var encryptionCertBase64 = builder.Configuration["OIDC_ENCRYPTION_CERT"];

                        if (!string.IsNullOrEmpty(signingCertBase64) && !string.IsNullOrEmpty(encryptionCertBase64))
                        {
                            var signingCert = System.Security.Cryptography.X509Certificates.X509CertificateLoader
                                .LoadPkcs12(Convert.FromBase64String(signingCertBase64), null);
                            var encryptionCert = System.Security.Cryptography.X509Certificates.X509CertificateLoader
                                .LoadPkcs12(Convert.FromBase64String(encryptionCertBase64), null);

                            options.AddSigningCertificate(signingCert)
                                   .AddEncryptionCertificate(encryptionCert);
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                "Production requires OIDC_SIGNING_CERT and OIDC_ENCRYPTION_CERT environment variables.");
                        }
                    }
                    else
                    {
                        options.AddDevelopmentEncryptionCertificate()
                               .AddDevelopmentSigningCertificate();
                    }

                    // Register the ASP.NET Core host
                    options.UseAspNetCore()
                           .EnableAuthorizationEndpointPassthrough()
                           .EnableTokenEndpointPassthrough()
                           .EnableUserinfoEndpointPassthrough()
                           .EnableLogoutEndpointPassthrough()
                           .EnableStatusCodePagesIntegration();
                })
                .AddValidation(options =>
                {
                    // Import config from the local OpenIddict server
                    options.UseLocalServer();
                    options.UseAspNetCore();
                    options.UseDataProtection();
                });

            // Use OpenIddict validation as the default authentication scheme
            // Cookie scheme is used for the login → authorize flow
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            })
            .AddCookie("EgibiCookie", options =>
            {
                options.Cookie.Name = "egibi.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None; // Required: Angular (www.egibi.io) ↔ API (api.egibi.io) are cross-origin
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Required when SameSite=None
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.Events.OnRedirectToLogin = context =>
                {
                    // For API calls, return 401. For browser navigations (authorize endpoint), redirect to SPA login.
                    if (context.Request.Path.StartsWithSegments("/connect"))
                    {
                        var loginUrl = builder.Configuration["Oidc:LoginRedirectUrl"] ?? "http://localhost:4200/auth/login";
                        context.Response.Redirect(loginUrl);
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                    }
                    return Task.CompletedTask;
                };
            });
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(egibi_api.Authorization.Policies.RequireAdmin, policy =>
                    policy.RequireClaim(OpenIddictConstants.Claims.Role, egibi_api.Authorization.UserRoles.Admin));
            });

            // --- Market Data Services ---
            builder.Services.AddSingleton<IOhlcRepository, OhlcRepository>();

            builder.Services.AddHttpClient<BinanceFetcher>(client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddSingleton<IMarketDataFetcher, BinanceFetcher>();
            // Future: builder.Services.AddSingleton<IMarketDataFetcher, CoinbaseFetcher>();

            builder.Services.AddSingleton<IMarketDataService, MarketDataService>();

            builder.Services.AddSignalR();

            // FIX #16: Set a reasonable upload limit instead of long.MaxValue (DoS vector)
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 104_857_600; // 100 MB
            });

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Initialize QuestDB ohlc table on startup (with retries for container startup timing)
            {
                var ohlcRepo = app.Services.GetRequiredService<IOhlcRepository>();
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                const int maxRetries = 5;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        await ohlcRepo.EnsureTableExistsAsync();
                        logger.LogInformation("QuestDB ohlc table initialized successfully.");
                        break;
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        logger.LogInformation("Waiting for QuestDB... attempt {Attempt}/{Max}", attempt, maxRetries);
                        await Task.Delay(3000);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not initialize QuestDB ohlc table after {Max} attempts. Is QuestDB running?", maxRetries);
                    }
                }
            }

            // Ensure database schema exists, seed admin + OIDC client
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EgibiDbContext>();
                var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                // Use the relational creator to check for and create tables.
                // This works correctly even when the database was created externally (Docker init).
                var creator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)
                    db.Database.GetInfrastructure()
                    .GetRequiredService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();

                try
                {
                    if (!await creator.HasTablesAsync())
                    {
                        await creator.CreateTablesAsync();
                        migrationLogger.LogInformation("Database tables created successfully.");
                    }
                    else
                    {
                        migrationLogger.LogInformation("Database tables already exist.");
                    }
                }
                catch (Exception ex)
                {
                    migrationLogger.LogError(ex, "Failed to create database tables.");
                    throw;
                }

                // Seed admin account
                var userService = scope.ServiceProvider.GetRequiredService<AppUserService>();
                await userService.SeedAdminAsync();

                // Seed OIDC client applications
                await SeedOidcClientsAsync(scope.ServiceProvider, builder.Configuration);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // FIX #11: Apply appropriate CORS policy per environment
            if (app.Environment.IsDevelopment())
            {
                app.UseCors(CorsPolicyDev);
            }
            else
            {
                app.UseCors(CorsPolicyProd);
            }

            // Health check for Railway
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

            // SignalR 
            //app.MapHub<ProgressHub>("/progressHub");
            app.MapHub<ChatHub>("/notificationHub");
            app.MapHub<FileUploadHub>("/fileUploadHub");

            // Only redirect in development — Railway/Cloudflare handle SSL termination in production
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Seeds the OpenIddict application entries (OIDC clients).
        /// Redirect URIs are configurable via appsettings.
        /// </summary>
        private static async Task SeedOidcClientsAsync(IServiceProvider services, IConfiguration configuration)
        {
            var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            // Read redirect URIs from configuration, with localhost defaults for development
            var redirectUris = configuration.GetSection("Oidc:RedirectUris").Get<string[]>()
                ?? new[] { "http://localhost:4200/auth/callback", "https://localhost:4200/auth/callback" };

            var postLogoutUris = configuration.GetSection("Oidc:PostLogoutRedirectUris").Get<string[]>()
                ?? new[] { "http://localhost:4200", "https://localhost:4200" };

            // --- egibi-ui (Angular SPA) ---
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "egibi-ui",
                DisplayName = "Egibi Trading Platform",
                ClientType = OpenIddictConstants.ClientTypes.Public, // SPA = public client (no secret)

                Permissions =
                {
                    // Endpoints
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Logout,

                    // Grant types
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                    // Response types
                    OpenIddictConstants.Permissions.ResponseTypes.Code,

                    // Scopes
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access"
                },

                // Require PKCE
                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                }
            };

            // Add configured redirect URIs
            foreach (var uri in redirectUris)
                descriptor.RedirectUris.Add(new Uri(uri));
            foreach (var uri in postLogoutUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));

            var existing = await manager.FindByClientIdAsync("egibi-ui");
            if (existing is null)
            {
                await manager.CreateAsync(descriptor);
                logger.LogInformation("Seeded OIDC client: egibi-ui");
            }
            else
            {
                // Update existing client to ensure permissions are current
                await manager.UpdateAsync(existing, descriptor);
                logger.LogInformation("Updated OIDC client: egibi-ui");
            }
        }
    }
}