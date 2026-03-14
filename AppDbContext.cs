using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CeaIndexer
{
    public class AppDbContext : DbContext
    {
        public DbSet<FileEntry> Files => Set<FileEntry>();
        public DbSet<Quantity> Quantities { get; set; }

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
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
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

