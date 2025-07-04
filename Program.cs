#nullable disable
using egibi_api.Data;
using egibi_api.Services;
using EgibiBinanceUsSdk;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

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
            builder.Services.AddScoped<ConfigurationService>();
            builder.Services.AddScoped<ExchangeAccountsService>();
            builder.Services.AddScoped(service => new QuestDbService(questDbConnectionString));



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

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();


            app.Run();
        }
    }
}
