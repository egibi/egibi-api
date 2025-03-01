#nullable disable

using egibi_api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.ComponentModel.DataAnnotations.Schema;
using egibi_api.Data.QuestEntities;

namespace egibi_api.Data
{
    public class EgibiQuestDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BinanceUsPriceData>()
                .Property(p => p.Timestamp)
                .HasColumnType("TIMESTAMP")
                .IsRequired();                                
        }

        public DbSet<BinanceUsPriceData> BinanceUsPriceData { get; set; }
    }

    
}
