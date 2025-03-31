using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TabletopTunes.Data;
using TabletopTunes.Entities;

namespace TabletopTunes.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly MusicPlayerDbContext _context;

        public TagRepository(MusicPlayerDbContext context)
        {
            _context = context;
        }

        public async Task<Tag> GetOrCreateTagAsync(string name)
        {
            name = name.Trim().ToLowerInvariant();
            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name == name);
            
            if (tag == null)
            {
                tag = new Tag 
                { 
                    Name = name
                };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }
            
            return tag;
        }

        public async Task<IEnumerable<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags
                .Include(t => t.TrackTags)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tag?> FindByNameAsync(string name)
        {
            name = name.Trim().ToLowerInvariant();
            return await _context.Tags
                .Include(t => t.TrackTags)
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<int> GetTrackCountForTagAsync(int tagId)
        {
            return await _context.TrackTags
                .CountAsync(tt => tt.TagId == tagId);
        }

        public async Task RenameTagAsync(int tagId, string newName)
        {
            newName = newName.Trim().ToLowerInvariant();
            var tag = await _context.Tags.FindAsync(tagId);
            if (tag != null)
            {
                tag.Name = newName;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MergeTagsAsync(int sourceTagId, int targetTagId)
        {
            if (sourceTagId == targetTagId) return;

            var sourceTag = await _context.Tags
                .Include(t => t.TrackTags)
                .FirstOrDefaultAsync(t => t.Id == sourceTagId);
                
            var targetTag = await _context.Tags.FindAsync(targetTagId);

            if (sourceTag != null && targetTag != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var trackTag in sourceTag.TrackTags.ToList())
                    {
                        var existingTrackTag = await _context.TrackTags
                            .FirstOrDefaultAsync(tt => 
                                tt.TrackId == trackTag.TrackId && 
                                tt.TagId == targetTagId);

                        if (existingTrackTag == null)
                        {
                            trackTag.TagId = targetTagId;
                            trackTag.Tag = targetTag;
                        }
                        else
                        {
                            _context.TrackTags.Remove(trackTag);
                        }
                    }

                    _context.Tags.Remove(sourceTag);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int limit = 10)
        {
            return await _context.Tags
                .Include(t => t.TrackTags)
                .OrderByDescending(t => t.TrackTags.Count)
                .Take(limit)
                .ToListAsync();
        }
    }
}
