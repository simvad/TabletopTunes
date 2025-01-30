using ReactiveUI;

namespace ModernMusicPlayer.ViewModels
{
    public class TagViewModel : ReactiveObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TrackCount { get; set; }
        
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
    }
}
