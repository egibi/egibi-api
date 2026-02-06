// FILE: egibi-api/Data/EgibiDbContext.cs

using egibi_api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using TimeZone = egibi_api.Data.Entities.TimeZone;

namespace egibi_api.Data
{
    public class EgibiDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Skip OpenIddict entities — they manage their own table names
                if (entityType.ClrType.Namespace?.StartsWith("OpenIddict") == true)
                    continue;

                modelBuilder.Entity(entityType.ClrType).ToTable(entityType.ClrType.Name);
            }

            // AppUser — unique email, relationship to credentials
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasMany(u => u.Credentials)
                    .WithOne(c => c.AppUser)
                    .HasForeignKey(c => c.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserCredential — composite unique constraint (one credential set per user per connection)
            modelBuilder.Entity<UserCredential>(entity =>
            {
                entity.HasIndex(c => new { c.AppUserId, c.ConnectionId }).IsUnique();

                entity.HasOne(c => c.Connection)
                    .WithMany()
                    .HasForeignKey(c => c.ConnectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Account>()
                .HasOne(ad => ad.AccountDetails)
                .WithOne(a => a.Account)
                .HasForeignKey<AccountDetails>(a => a.AccountId);

            //modelBuilder.Entity<Account>()
            //    .HasOne(s => s.AccountSecurityDetails)
            //    .WithOne(a => a.Account)
            //    .HasForeignKey<AccountSecurityDetails>(a => a.AccountId);

            //modelBuilder.Entity<Account>()
            //    .HasOne(api => api.AccountApiDetails)
            //    .WithOne(a => a.Account)
            //    .HasForeignKey<AccountApiDetails>(a => a.AccountId);

            //modelBuilder.Entity<Account>()
            //    .HasOne(fees => fees.AccountFeeStructureDetails)
            //    .WithOne(a => a.Account)
            //    .HasForeignKey<AccountFeeStructureDetails>(a => a.AccountId);

            //modelBuilder.Entity<Account>()
            //    .HasOne(status => status.AccountStatusDetails)
            //    .WithOne(a => a.Account)
            //    .HasForeignKey<AccountStatusDetails>(a => a.AccountId);


            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConnectionType>().HasData(DbSetup.GetConnectionTypes());
            modelBuilder.Entity<Connection>().HasData(DbSetup.GetConnections());
            modelBuilder.Entity<DataFormatType>().HasData(DbSetup.GetDataFormatTypes());
            modelBuilder.Entity<DataFrequencyType>().HasData(DbSetup.GetDataFrequencyTypes());
            modelBuilder.Entity<DataProviderType>().HasData(DbSetup.GetDataProviderTypes());
            modelBuilder.Entity<BacktestStatus>().HasData(DbSetup.GetBacktestStatuses());
        }

        public EgibiDbContext(DbContextOptions<EgibiDbContext> options) : base(options) { }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<AppConfiguration> AppConfigurations { get; set; }
        public DbSet<UserCredential> UserCredentials { get; set; }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<AccountUser> AccountUsers { get; set; }
        public DbSet<AccountDetails> AccountDetails { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<ConnectionType> ConnectionTypes { get; set; }
        public DbSet<Strategy> Strategies { get; set; }
        public DbSet<Backtest> Backtests { get; set; }
        public DbSet<BacktestStatus> BacktestStatuses { get; set; }
        public DbSet<DataProvider> DataProviders { get; set; }
        public DbSet<DataProviderType> DataProviderTypes { get; set; }
        public DbSet<DataFormatType> DataFormatTypes { get; set; }
        public DbSet<DataFrequencyType> DataFrequencyTypes { get; set; }
        public DbSet<Exchange> Exchanges { get; set; }
        public DbSet<ExchangeType> ExchangeTypes { get; set; }
        public DbSet<Market> Markets { get; set; }
        public DbSet<MarketType> MarketTypes { get; set; }
        public DbSet<ExchangeFeeStructure> ExchangeFeeStructures { get; set; }
        public DbSet<ExchangeFeeStructureTier> ExchangeFeeStructureTiers { get; set; }
        public DbSet<ExchangeAccount> ExchangeAccounts { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<TimeZone> TimeZones { get; set; }
        public DbSet<AccountFeeStructureDetails> AccountFeeStructureDetails { get; set; }
    }
}
