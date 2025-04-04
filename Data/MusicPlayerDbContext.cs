using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using TabletopTunes.Entities;

namespace TabletopTunes.Data
{
    public class MusicPlayerDbContext : DbContext
    {
        public DbSet<TrackEntity> Tracks { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<TrackTag> TrackTags { get; set; } = null!;

        public string DbPath { get; private set; }

        public MusicPlayerDbContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "TabletopTunes", "musicplayer.db");
            
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DbPath)!);
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
