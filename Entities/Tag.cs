using System.Collections.Generic;

namespace ModernMusicPlayer.Entities
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<TrackTag> TrackTags { get; set; } = new List<TrackTag>();
    }
}
