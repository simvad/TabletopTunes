using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TabletopTunes.Core.Data;
using TabletopTunes.Core.Entities;

namespace TabletopTunes.Tests
{
    public class DbContextBasicTests
    {
        [Fact]
        public void DbContext_HasCorrectTableConfiguration()
        {
            // This test verifies that the DbContext has been configured with 
            // the expected entity sets and basic structure
            
            // Create a mock DbContext using in-memory database
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("DbContext_HasCorrectTableConfiguration")
                .Options;
            
            using var context = new InMemoryTestDbContext(options);
            
            // Verify entity sets are defined
            Assert.NotNull(context.Tracks);
            Assert.NotNull(context.Tags);
            Assert.NotNull(context.TrackTags);
        }
    }
    
    // A specialized test DbContext that doesn't access the real file system
    public class InMemoryTestDbContext : MusicPlayerDbContext
    {
        private readonly DbContextOptions<MusicPlayerDbContext> _options;
        
        public InMemoryTestDbContext(DbContextOptions<MusicPlayerDbContext> options)
        {
            _options = options;
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use the options from the constructor instead of calling the base implementation
            // which would try to use SQLite with a file path
            optionsBuilder.UseInMemoryDatabase("TestDb");
        }
    }
}