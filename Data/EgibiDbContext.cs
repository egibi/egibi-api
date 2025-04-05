using egibi_api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Data
{
    public class EgibiDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach(var entityType in modelBuilder.Model.GetEntityTypes())
            {
                modelBuilder.Entity(entityType.ClrType).ToTable(entityType.ClrType.Name);
            }

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConnectionType>().HasData(DbSetup.GetConnectionTypes());
            modelBuilder.Entity<Connection>().HasData(DbSetup.GetConnections());
        }

        public EgibiDbContext(DbContextOptions<EgibiDbContext> options) : base(options) { }
        
        public DbSet<Connection> Connections { get; set; }
        public DbSet<ConnectionType> ConnectionTypes { get; set; }
        public DbSet<Strategy> Strategies { get; set; }
        public DbSet<Backtest> Backtests { get; set; }
    }
}
