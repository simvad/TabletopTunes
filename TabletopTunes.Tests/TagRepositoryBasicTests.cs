using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TabletopTunes.Core.Data;
using TabletopTunes.Core.Entities;
using TabletopTunes.Core.Repositories;

namespace TabletopTunes.Tests
{
    public class TagRepositoryBasicTests
    {
        [Fact]
        public async Task GetOrCreateTagAsync_CreateNewTag_SavesToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MusicPlayerDbContext>()
                .UseInMemoryDatabase("GetOrCreateTagAsync_CreateNewTag_SavesToDatabase")
                .Options;
            
            // Act & Assert
            using (var context = new InMemoryTestDbContext(options))
            {
                var repository = new TagRepository(context);
                
                // First, check that the tag doesn't exist
                var tagsBeforeCount = await context.Tags.CountAsync();
                Assert.Equal(0, tagsBeforeCount);
                
                // Create the tag
                var tag = await repository.GetOrCreateTagAsync("newTag");
                
                // Verify tag properties
                Assert.NotNull(tag);
                Assert.Equal("newtag", tag.Name); // Tag names are stored in lowercase
                Assert.NotEqual(0, tag.Id); // Should have an ID
                
                // Verify it was saved to the database
                var tagsAfterCount = await context.Tags.CountAsync();
                Assert.Equal(1, tagsAfterCount);
            }
        }
    }
}