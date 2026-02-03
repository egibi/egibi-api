#nullable disable
using egibi_api.Data;
using egibi_api.Hubs;
using egibi_api.Services;
using EgibiBinanceUsSdk;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using egibi_api.Services.Security;

namespace egibi_api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var CorsPolicyDev = "_corsPolicyDev";
            var builder = WebApplication.CreateBuilder(args);
            string env = builder.Environment.EnvironmentName;
            string dbConnectionString = string.Empty;
            string questDbConnectionString = string.Empty;

            builder.Services.Configure<ConfigOptions>(builder.Configuration.GetSection("ConfigOptions"));

            Console.WriteLine($"env={builder.Environment.EnvironmentName}");



            // --- Encryption Service ---
            // Master key lookup order: appsettings → environment variable → fail
            string masterKey = builder.Configuration["Encryption:MasterKey"]
                ?? Environment.GetEnvironmentVariable("EGIBI_MASTER_KEY");

            if (string.IsNullOrWhiteSpace(masterKey))
            {
                if (builder.Environment.IsDevelopment())
                {
                    // Auto-generate a dev key on first run (logged to console so you can save it)
                    masterKey = EncryptionService.GenerateMasterKey();
                    Console.WriteLine("==========================================================");
                    Console.WriteLine("WARNING: No master encryption key configured.");
                    Console.WriteLine("A temporary key has been generated for this session:");
                    Console.WriteLine(masterKey);
                    Console.WriteLine("Save this in appsettings.Development.json under:");
                    Console.WriteLine("  \"Encryption\": { \"MasterKey\": \"<paste here>\" }");
                    Console.WriteLine("Data encrypted with this key will be unreadable if lost.");
                    Console.WriteLine("==========================================================");
                }
                else
                {
                    throw new InvalidOperationException(
                        "Master encryption key not configured. " +
                        "Set 'Encryption:MasterKey' in config or EGIBI_MASTER_KEY environment variable.");
                }
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
                questDbConnectionString = builder.Configuration.GetConnectionString("EgibiQuestDb");

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

            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IEncryptionService>(new EncryptionService(masterKey));


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
