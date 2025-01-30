using System.Threading.Tasks;
using System.Collections.Generic;
using ModernMusicPlayer.Entities;

namespace ModernMusicPlayer.Repositories
{
    public interface ITrackRepository
    {
        Task<TrackEntity?> GetByIdAsync(string id);
        Task<IEnumerable<TrackEntity>> GetAllAsync();
        Task<IEnumerable<TrackEntity>> SearchAsync(string query);
        Task<IEnumerable<TrackEntity>> GetByTagsAsync(IEnumerable<string> tags, bool matchAll = true);
        Task<TrackEntity> AddAsync(TrackEntity track);
        Task UpdateAsync(TrackEntity track);
        Task DeleteAsync(string id);
    }
}
