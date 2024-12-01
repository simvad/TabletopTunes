using System.Linq;
using ModernMusicPlayer.Entities;

namespace ModernMusicPlayer.Query
{
    public static class TrackQueryExtensions
    {
        public static IQueryable<TrackEntity> ApplyQuery(this IQueryable<TrackEntity> query, string searchQuery)
        {
            var parser = new TrackQueryParser();
            var predicate = parser.ParseQuery(searchQuery);
            return query.Where(predicate);
        }
    }
}