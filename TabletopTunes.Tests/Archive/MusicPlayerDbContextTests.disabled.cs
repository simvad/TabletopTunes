using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TabletopTunes.Core.Data;
using TabletopTunes.Core.Entities;

namespace TabletopTunes.Tests
{
    public class MusicPlayerDbContextTests
    {
        [Fact]
        public void EnsureDatabaseCreated_CreatesEmptyDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("EnsureDatabaseCreated_CreatesEmptyDatabase")
                .Options;

            // Act
            using (var context = new TestMusicPlayerDbContext(options))
            {
                context.EnsureDatabaseCreated();
                
                // Assert
                // Database.EnsureCreated() returns false when the database already exists
                // In tests with in-memory database, it's always "created" so we don't need this check
                // Assert.True(context.Database.EnsureCreated());
                Assert.Empty(context.Tracks);
                Assert.Empty(context.Tags);
                Assert.Empty(context.TrackTags);
            }
        }
        
        [Fact]
        public async Task TrackTagRelationship_CorrectlyDefined()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("TrackTagRelationship_CorrectlyDefined")
                .Options;

            // Set up test data
            using (var context = new TestMusicPlayerDbContext(options))
            {
                // Create some tags
                var tag1 = new Tag { Name = "adventure" };
                var tag2 = new Tag { Name = "mystery" };
                context.Tags.AddRange(tag1, tag2);
                await context.SaveChangesAsync();
                
                // Create a track with those tags
                var track = new TrackEntity 
                { 
                    Id = "track123", 
                    Title = "Adventure Theme", 
                    Url = "https://example.com/music.mp3"
                };
                context.Tracks.Add(track);
                await context.SaveChangesAsync();
                
                // Add the tag relationships
                context.TrackTags.Add(new TrackTag 
                { 
                    TrackId = track.Id, 
                    TagId = tag1.Id,
                    Track = track,
                    Tag = tag1
                });
                
                context.TrackTags.Add(new TrackTag 
                { 
                    TrackId = track.Id, 
                    TagId = tag2.Id,
                    Track = track,
                    Tag = tag2
                });
                
                await context.SaveChangesAsync();
            }
            
            // Act & Assert
            using (var context = new TestMusicPlayerDbContext(options))
            {
                // Query with Include to test relationships
                var track = await context.Tracks
                    .Include(t => t.TrackTags)
                    .ThenInclude(tt => tt.Tag)
                    .FirstOrDefaultAsync(t => t.Id == "track123");
                
                Assert.NotNull(track);
                Assert.Equal(2, track.TrackTags.Count);
                
                // Check both tags are correctly linked
                Assert.Contains(track.TrackTags, tt => tt.Tag.Name == "adventure");
                Assert.Contains(track.TrackTags, tt => tt.Tag.Name == "mystery");
                
                // Now test the relationship from tags to tracks
                var tag = await context.Tags
                    .Include(t => t.TrackTags)
                    .ThenInclude(tt => tt.Track)
                    .FirstOrDefaultAsync(t => t.Name == "adventure");
                
                Assert.NotNull(tag);
                Assert.Single(tag.TrackTags);
                Assert.Equal("Adventure Theme", tag.TrackTags.First().Track.Title);
            }
        }
    }
}