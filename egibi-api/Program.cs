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

namespace egibi_api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var CorsPolicyDev = "_corsPolicyDev";
            var builder = WebApplication.CreateBuilder(args);
            string env = builder.Environment.EnvironmentName;
            string dbConnectionString = string.Empty;
            string questDbConnectionString = string.Empty;

            builder.Services.Configure<ConfigOptions>(builder.Configuration.GetSection("ConfigOptions"));

            // QuestDB options binding
            builder.Services.Configure<QuestDbOptions>(
                builder.Configuration.GetSection(QuestDbOptions.SectionName));

            Console.WriteLine($"env={builder.Environment.EnvironmentName}");

            // Auto-start Docker databases in Development
            if (builder.Environment.IsDevelopment())
            {
                await DevInfrastructure.EnsureDatabasesAsync(builder.Environment.ContentRootPath);
            }

            if (builder.Environment.IsProduction())
            {
                // TODO: Setup production connection strings when ready
                dbConnectionString = builder.Configuration.GetConnectionString("prod_connectionstring");
                questDbConnectionString = builder.Configuration.GetConnectionString("EgibiQuestDb");

                builder.Services.AddDbContextPool<EgibiDbContext>(options =>
                    options.UseNpgsql(builder.Configuration.GetConnectionString(dbConnectionString)));
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
                            policy.AllowAnyOrigin();
                            policy.AllowAnyHeader();
                            policy.AllowAnyMethod();
                        });
                });
            }

            // Add services to the container.
            builder.Services.AddHttpClient<BinanceUsHttpClient>();
            builder.Services.AddScoped<ApiTesterService>();
            builder.Services.AddScoped<ConnectionsService>();
            builder.Services.AddScoped<DataManagerService>();
            builder.Services.AddScoped<StrategiesService>();
            builder.Services.AddScoped<BacktesterService>();
            builder.Services.AddScoped<ExchangesService>();
            builder.Services.AddScoped<MarketsService>();
            builder.Services.AddScoped<AppConfigurationsService>();
            builder.Services.AddScoped<ExchangeAccountsService>();
            builder.Services.AddScoped<AccountsService>();
            builder.Services.AddScoped(service => new QuestDbService(questDbConnectionString));
            builder.Services.AddScoped<TestingService>();
            builder.Services.AddScoped<GeoDateTimeDataService>();

            // --- Security & Auth ---
            var masterKey = builder.Configuration["Encryption:MasterKey"];
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

                    // Register scopes
                    options.RegisterScopes(
                        OpenIddictConstants.Scopes.Email,
                        OpenIddictConstants.Scopes.Profile,
                        OpenIddictConstants.Scopes.Roles,
                        "api");

                    // Use ASP.NET Core Data Protection for token format (development-friendly)
                    options.UseDataProtection();

                    // Development: ephemeral keys (regenerated on restart)
                    // TODO: Replace with persistent certificates for production
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();

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
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.Events.OnRedirectToLogin = context =>
                {
                    // Return 401 instead of redirecting (API-friendly)
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });
            builder.Services.AddAuthorization();

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


            // Allow large form limits. Will need to handle differently in future if hosted non-locally
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = long.MaxValue;
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
                    db.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();

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
                await SeedOidcClientsAsync(scope.ServiceProvider);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (builder.Environment.IsDevelopment())
            {
                app.UseCors(CorsPolicyDev);
            }

            // SignalR 
            //app.MapHub<ProgressHub>("/progressHub");
            app.MapHub<ChatHub>("/notificationHub");
            app.MapHub<FileUploadHub>("/fileUploadHub");

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();


            app.Run();
        }

        /// <summary>
        /// Seeds the OpenIddict application entries (OIDC clients).
        /// </summary>
        private static async Task SeedOidcClientsAsync(IServiceProvider services)
        {
            var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            // --- egibi-ui (Angular SPA) ---
            if (await manager.FindByClientIdAsync("egibi-ui") is null)
            {
                await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "egibi-ui",
                    DisplayName = "Egibi Trading Platform",
                    ClientType = OpenIddictConstants.ClientTypes.Public, // SPA = public client (no secret)

                    // Redirect URIs for the Angular app
                    RedirectUris =
                    {
                        new Uri("http://localhost:4200/auth/callback"),
                        new Uri("https://localhost:4200/auth/callback")
                    },
                    PostLogoutRedirectUris =
                    {
                        new Uri("http://localhost:4200"),
                        new Uri("https://localhost:4200")
                    },

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
                        OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                    },

                    // Require PKCE
                    Requirements =
                    {
                        OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                    }
                });

                logger.LogInformation("Seeded OIDC client: egibi-ui");
            }
        }
    }
}