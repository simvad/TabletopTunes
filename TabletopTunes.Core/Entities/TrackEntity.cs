using System;
using System.Collections.Generic;

namespace TabletopTunes.Core.Entities
{
    public class TrackEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public virtual ICollection<TrackTag> TrackTags { get; set; } = new List<TrackTag>();
    }
}