using egibi_api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Data
{
    public class EgibiDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                modelBuilder.Entity(entityType.ClrType).ToTable(entityType.ClrType.Name);
            }

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
        }

        public EgibiDbContext(DbContextOptions<EgibiDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<AccountDetails> AccountDetails { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<ConnectionType> ConnectionTypes { get; set; }
        public DbSet<Strategy> Strategies { get; set; }
        public DbSet<Backtest> Backtests { get; set; }
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
    }
}
