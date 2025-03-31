namespace TabletopTunes.Entities
{
    public class TrackTag
    {
        public string TrackId { get; set; } = string.Empty;
        public int TagId { get; set; }
        
        public virtual TrackEntity Track { get; set; } = null!;
        public virtual Tag Tag { get; set; } = null!;
    }
}
