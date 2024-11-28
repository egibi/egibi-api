using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace egibi_api.Data
{
    public class EgibiDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public EgibiDbContext(DbContextOptions<EgibiDbContext> options) : base(options) { }
    }
}
