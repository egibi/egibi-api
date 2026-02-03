#nullable disable
using egibi_api.Configuration;
using egibi_api.Data;
using egibi_api.Hubs;
using egibi_api.MarketData.Fetchers;
using egibi_api.MarketData.Repositories;
using egibi_api.MarketData.Services;
using egibi_api.Services;
using EgibiBinanceUsSdk;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

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

                builder.Services.AddDbContextPool<EgibiDbContext>(options =>
                    options.UseNpgsql(dbConnectionString));

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

            // Initialize QuestDB ohlc table on startup
            try
            {
                var ohlcRepo = app.Services.GetRequiredService<IOhlcRepository>();
                await ohlcRepo.EnsureTableExistsAsync();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(ex, "Could not initialize QuestDB ohlc table. Is QuestDB running?");
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
            app.UseAuthorization();
            app.MapControllers();


            app.Run();
        }
    }
}
