using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TabletopTunes.Core.Data;
using TabletopTunes.Core.Entities;
using TabletopTunes.Core.Repositories;

namespace TabletopTunes.Tests
{
    public class TrackRepositoryTests
    {
        [Fact]
        public async Task GetTrackById_ReturnsCorrectTrack()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("GetTrackById_ReturnsCorrectTrack")
                .Options;

            string trackId = "track123";
            
            // Set up the database
            using (var context = new TestMusicPlayerDbContext(options))
            {
                context.Tracks.Add(new TrackEntity 
                { 
                    Id = trackId, 
                    Title = "Battle Theme", 
                    Url = "https://example.com/music1.mp3" 
                });
                
                context.Tracks.Add(new TrackEntity 
                { 
                    Id = "track456", 
                    Title = "Peaceful Melody", 
                    Url = "https://example.com/music2.mp3" 
                });
                
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var repository = new TrackRepository(context);
                var track = await repository.GetByIdAsync(trackId);

                // Assert
                Assert.NotNull(track);
                Assert.Equal(trackId, track.Id);
                Assert.Equal("Battle Theme", track.Title);
                Assert.Equal("https://example.com/music1.mp3", track.Url);
            }
        }

        [Fact]
        public async Task AddTrack_SavesTrackWithTags()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("AddTrack_SavesTrackWithTags")
                .Options;

            // Create tags first
            using (var context = new TestMusicPlayerDbContext(options))
            {
                context.Tags.Add(new Tag { Id = 1, Name = "epic" });
                context.Tags.Add(new Tag { Id = 2, Name = "battle" });
                await context.SaveChangesAsync();
            }

            // Act - Add a track with tags
            string trackId = "track789";
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var repository = new TrackRepository(context);
                
                var track = new TrackEntity
                {
                    Id = trackId,
                    Title = "Epic Battle Theme",
                    Url = "https://example.com/music3.mp3",
                    TrackTags = new List<TrackTag>
                    {
                        new TrackTag { TagId = 1 },
                        new TrackTag { TagId = 2 }
                    }
                };
                
                await repository.AddAsync(track);
            }

            // Assert
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var savedTrack = await context.Tracks
                    .Include(t => t.TrackTags)
                    .ThenInclude(tt => tt.Tag)
                    .FirstAsync(t => t.Id == trackId);
                
                Assert.Equal("Epic Battle Theme", savedTrack.Title);
                Assert.Equal(2, savedTrack.TrackTags.Count);
                Assert.Contains(savedTrack.TrackTags, tt => tt.Tag.Name == "epic");
                Assert.Contains(savedTrack.TrackTags, tt => tt.Tag.Name == "battle");
            }
        }

        [Fact]
        public async Task UpdateTrack_UpdatesExistingTrack()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("UpdateTrack_UpdatesExistingTrack")
                .Options;

            string trackId = "track101";
            
            // Set up the database
            using (var context = new TestMusicPlayerDbContext(options))
            {
                context.Tracks.Add(new TrackEntity 
                { 
                    Id = trackId, 
                    Title = "Original Title", 
                    Url = "https://example.com/old.mp3" 
                });
                
                await context.SaveChangesAsync();
            }

            // Act - Update the track
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var repository = new TrackRepository(context);
                var track = await context.Tracks.FindAsync(trackId);
                
                if (track != null)
                {
                    // Update properties
                    track.Title = "Updated Title";
                    track.Url = "https://example.com/new.mp3";
                    
                    await repository.UpdateAsync(track);
                }
            }

            // Assert
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var updatedTrack = await context.Tracks.FindAsync(trackId);
                
                Assert.NotNull(updatedTrack);
                if (updatedTrack != null)
                {
                    Assert.Equal("Updated Title", updatedTrack.Title);
                    Assert.Equal("https://example.com/new.mp3", updatedTrack.Url);
                }
            }
        }
    }
}