using System;
using System.Collections.Generic;

namespace ModernMusicPlayer.Entities
{
    public class TrackEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPlayedAt { get; set; }
        public int PlayCount { get; set; }
        public string? CachePath { get; set; }
        public virtual ICollection<TrackTag> TrackTags { get; set; } = new List<TrackTag>();
    }
}