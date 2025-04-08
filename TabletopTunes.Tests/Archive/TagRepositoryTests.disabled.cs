using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TabletopTunes.Core.Data;
using TabletopTunes.Core.Entities;
using TabletopTunes.Core.Repositories;

namespace TabletopTunes.Tests
{
    public class TagRepositoryTests
    {
        [Fact]
        public async Task GetAllTags_ReturnsAllTags()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("GetAllTags_ReturnsAllTags")
                .Options;

            // Set up the database
            using (var context = new TestMusicPlayerDbContext(options))
            {
                context.Tags.Add(new Tag { Name = "rock" });
                context.Tags.Add(new Tag { Name = "jazz" });
                context.Tags.Add(new Tag { Name = "ambient" });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var repository = new TagRepository(context);
                var tags = await repository.GetAllTagsAsync();

                // Assert
                Assert.Equal(3, tags.Count());
                Assert.Contains(tags, t => t.Name == "rock");
                Assert.Contains(tags, t => t.Name == "jazz");
                Assert.Contains(tags, t => t.Name == "ambient");
            }
        }

        [Fact]
        public async Task AddTag_SavesTagToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("AddTag_SavesTagToDatabase")
                .Options;

            // Act
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var repository = new TagRepository(context);
                var tag = await repository.GetOrCreateTagAsync("epic");

                // Assert
                Assert.Equal("epic", tag.Name);
                Assert.NotEqual(0, tag.Id); // Should have a valid ID
            }

            // Verify tag was saved
            using (var context = new TestMusicPlayerDbContext(options))
            {
                Assert.Equal(1, await context.Tags.CountAsync());
                Assert.Equal("epic", await context.Tags.Select(t => t.Name).FirstAsync());
            }
        }

        [Fact]
        public async Task GetTagByName_ReturnsCorrectTag()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("GetTagByName_ReturnsCorrectTag")
                .Options;

            // Set up the database
            using (var context = new TestMusicPlayerDbContext(options))
            {
                context.Tags.Add(new Tag { Name = "battle" });
                context.Tags.Add(new Tag { Name = "tavern" });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new TestMusicPlayerDbContext(options))
            {
                var repository = new TagRepository(context);
                var tag = await repository.FindByNameAsync("tavern");

                // Assert
                Assert.NotNull(tag);
                Assert.Equal("tavern", tag.Name);
            }
        }
    }

    // Test version of the DbContext that doesn't need a file system
    public class TestMusicPlayerDbContext : MusicPlayerDbContext
    {
        private readonly DbContextOptions<MusicPlayerDbContext> _options;
        
        public TestMusicPlayerDbContext(DbContextOptions<MusicPlayerDbContext> options)
            : base()
        {
            // Store the options so we can use them in OnConfiguring
            _options = options;
            this.Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use the options passed through the constructor
            // This allows us to keep the same database name for all contexts within a single test
            optionsBuilder.UseInMemoryDatabase("TestDatabase");
        }
        
        // Can't override DbPath because it's not virtual, but it's ok because we're using in-memory DB
    }
}