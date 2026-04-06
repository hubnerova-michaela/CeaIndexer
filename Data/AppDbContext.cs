using CeaIndexer.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CeaIndexer.Data
{
    public class AppDbContext : DbContext
    {

        public DbSet<FileEntry> Files { get; set; }
        public DbSet<MeasurePoint> MeasurePoints { get; set; }
        public DbSet<QuantityItem> Quantities { get; set; }
        public DbSet<Archive> Archives { get; set; }

        private static string GetDbPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "index.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = GetDbPath();
                optionsBuilder.UseSqlite($"Data Source={dbPath}")

                // 1. ZAPNUTÍ LOGOVÁNÍ DO VS
                .LogTo(message => System.Diagnostics.Debug.WriteLine(message), Microsoft.Extensions.Logging.LogLevel.Information)

                // 2. ZOBRAZENÍ KONKRÉTNÍCH HODNOT
                .EnableSensitiveDataLogging();

            }
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileEntry>()
                .HasIndex(f => f.Path)
                .IsUnique();

        }
    }
}

