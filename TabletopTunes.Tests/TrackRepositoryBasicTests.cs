using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TabletopTunes.Core.Data;
using TabletopTunes.Core.Entities;
using TabletopTunes.Core.Repositories;

namespace TabletopTunes.Tests
{
    public class TrackRepositoryBasicTests
    {
        [Fact]
        public async Task AddAsync_SavesTrackToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("AddAsync_SavesTrackToDatabase")
                .Options;
            
            // Act
            using (var context = new InMemoryTestDbContext(options))
            {
                var repository = new TrackRepository(context);
                
                // Ensure the database is empty
                var tracksBeforeCount = await context.Tracks.CountAsync();
                Assert.Equal(0, tracksBeforeCount);
                
                // Create and add a track
                var track = new TrackEntity
                {
                    Id = "test-track-123",
                    Title = "Test Track",
                    Url = "https://example.com/test.mp3"
                };
                
                var savedTrack = await repository.AddAsync(track);
                
                // Assert
                Assert.NotNull(savedTrack);
                Assert.Equal("test-track-123", savedTrack.Id);
                Assert.Equal("Test Track", savedTrack.Title);
                
                // Verify it was saved to the database
                var tracksAfterCount = await context.Tracks.CountAsync();
                Assert.Equal(1, tracksAfterCount);
                
                var retrievedTrack = await context.Tracks.FirstOrDefaultAsync();
                Assert.NotNull(retrievedTrack);
                Assert.Equal("Test Track", retrievedTrack.Title);
            }
        }
    }
}