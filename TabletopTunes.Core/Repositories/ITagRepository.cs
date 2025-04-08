using System.Threading.Tasks;
using System.Collections.Generic;
using TabletopTunes.Core.Entities;

namespace TabletopTunes.Core.Repositories
{
    public interface ITagRepository
    {
        Task<Tag> GetOrCreateTagAsync(string name);
        Task<IEnumerable<Tag>> GetAllTagsAsync();
        Task<Tag?> FindByNameAsync(string name);
        Task<int> GetTrackCountForTagAsync(int tagId);
        Task RenameTagAsync(int tagId, string newName);
        Task MergeTagsAsync(int sourceTagId, int targetTagId);
        Task<IEnumerable<Tag>> GetPopularTagsAsync(int limit = 10);
    }
}