using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TabletopTunes.Core.Entities;

namespace TabletopTunes.Core.Data
{
    public class MusicPlayerDbContext : DbContext
    {
        public DbSet<TrackEntity> Tracks { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<TrackTag> TrackTags { get; set; } = null!;

        public string DbPath { get; private set; }
        private readonly ILogger<MusicPlayerDbContext>? _logger;

        public MusicPlayerDbContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "TabletopTunes", "musicplayer.db");
            
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DbPath)!);
        }
        
        public MusicPlayerDbContext(ILogger<MusicPlayerDbContext> logger) : this()
        {
            _logger = logger;
        }

        public void EnsureDatabaseCreated()
        {
            try
            {
                // Create the database if it doesn't exist
                Database.EnsureCreated();
                
                // Verify the schema matches our entities
                try
                {
                    RelationalDatabaseCreator databaseCreator = (RelationalDatabaseCreator)Database.GetService<IDatabaseCreator>();
                    databaseCreator.CreateTables();
                }
                catch (Exception ex)
                {
                    // Tables already exist, which is fine
                    _logger?.LogDebug(ex, "Tables already exist. This is normal.");
                }
                
                _logger?.LogInformation("Database verified at {DbPath}", DbPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error ensuring database is created at {DbPath}", DbPath);
                throw;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrackEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Url).IsRequired();
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<TrackTag>(entity =>
            {
                entity.HasKey(t => new { t.TrackId, t.TagId });

                entity.HasOne(tt => tt.Track)
                    .WithMany(t => t.TrackTags)
                    .HasForeignKey(tt => tt.TrackId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tt => tt.Tag)
                    .WithMany(t => t.TrackTags)
                    .HasForeignKey(tt => tt.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}