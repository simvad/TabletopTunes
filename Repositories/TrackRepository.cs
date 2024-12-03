using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ModernMusicPlayer.Data;
using ModernMusicPlayer.Entities;
using ModernMusicPlayer.Query;

namespace ModernMusicPlayer.Repositories
{
    public class TrackRepository : ITrackRepository
    {
        private readonly MusicPlayerDbContext _context;

        public TrackRepository(MusicPlayerDbContext context)
        {
            _context = context;
        }

        public async Task<TrackEntity?> GetByIdAsync(string id)
        {
            return await _context.Tracks
                .Include(t => t.TrackTags)
                .ThenInclude(tt => tt.Tag)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TrackEntity>> GetAllAsync()
        {
            return await _context.Tracks
                .Include(t => t.TrackTags)
                .ThenInclude(tt => tt.Tag)
                .ToListAsync();
        }

        public async Task<IEnumerable<TrackEntity>> SearchAsync(string query)
        {
            query = query.ToLower();
            return await _context.Tracks
                .Include(t => t.TrackTags)
                .ThenInclude(tt => tt.Tag)
                .Where(t => t.Title.ToLower().Contains(query) ||
                           t.TrackTags.Any(tt => tt.Tag.Name.ToLower().Contains(query)))
                .ToListAsync();
        }

        public async Task<IEnumerable<TrackEntity>> GetByTagsAsync(IEnumerable<string> tags, bool matchAll = true)
        {
            var query = _context.Tracks
                .Include(t => t.TrackTags)
                .ThenInclude(tt => tt.Tag)
                .AsQueryable();

            var tagList = tags.Select(t => t.ToLower()).ToList();

            if (matchAll)
            {
                foreach (var tag in tagList)
                {
                    query = query.Where(t => t.TrackTags.Any(tt => tt.Tag.Name.ToLower() == tag));
                }
            }
            else
            {
                query = query.Where(t => t.TrackTags.Any(tt => tagList.Contains(tt.Tag.Name.ToLower())));
            }

            return await query.ToListAsync();
        }

        public async Task<TrackEntity> AddAsync(TrackEntity track)
        {
            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();
            
            // Reload the track with its relationships
            return await _context.Tracks
                .Include(t => t.TrackTags)
                .ThenInclude(tt => tt.Tag)
                .FirstAsync(t => t.Id == track.Id);
        }

        public async Task UpdateAsync(TrackEntity track)
        {
            _context.Entry(track).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track != null)
            {
                _context.Tracks.Remove(track);
                await _context.SaveChangesAsync();
            }
        }
    }
}
