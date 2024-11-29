using System;
using System.Collections.Generic;

namespace ModernMusicPlayer.Entities
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual ICollection<TrackTag> TrackTags { get; set; } = new List<TrackTag>();
    }
}