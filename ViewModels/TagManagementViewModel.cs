using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ModernMusicPlayer.Commands;
using ModernMusicPlayer.Entities;
using ModernMusicPlayer.Repositories;
using ReactiveUI;

namespace ModernMusicPlayer.ViewModels
{
    public class TagManagementViewModel : ReactiveObject
    {
        private readonly ITagRepository _tagRepository;
        private readonly ITrackRepository _trackRepository;

        private ObservableCollection<TagViewModel> _availableTags = new();
        public ObservableCollection<TagViewModel> AvailableTags => _availableTags;

        private bool _isEditTagsOpen;
        public bool IsEditTagsOpen
        {
            get => _isEditTagsOpen;
            set => this.RaiseAndSetIfChanged(ref _isEditTagsOpen, value);
        }

        private TrackEntity? _editingTrack;
        public TrackEntity? EditingTrack
        {
            get => _editingTrack;
            set => this.RaiseAndSetIfChanged(ref _editingTrack, value);
        }

        private string _editingTags = "";
        public string EditingTags
        {
            get => _editingTags;
            set => this.RaiseAndSetIfChanged(ref _editingTags, value);
        }

        public ICommand? EditTrackTagsCommand { get; private set; }
        public ICommand? SaveTagsCommand { get; private set; }
        public ICommand? CloseEditTagsCommand { get; private set; }

        public event EventHandler? TagsChanged;

        public TagManagementViewModel(
            ITagRepository tagRepository,
            ITrackRepository trackRepository)
        {
            _tagRepository = tagRepository;
            _trackRepository = trackRepository;

            InitializeCommands();
            _ = RefreshTagsAsync();
        }

        private void InitializeCommands()
        {
            EditTrackTagsCommand = new RelayCommand<TrackEntity>(track =>
            {
                EditingTrack = track;
                EditingTags = string.Join(", ", track?.TrackTags.Select(tt => tt.Tag.Name) ?? Array.Empty<string>());
                IsEditTagsOpen = true;
            });

            SaveTagsCommand = new RelayCommand(async () =>
            {
                if (EditingTrack != null)
                {
                    try
                    {
                        EditingTrack.TrackTags.Clear();
                        var tagNames = EditingTags.Split(',')
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t));

                        foreach (var tagName in tagNames)
                        {
                            var tag = await _tagRepository.GetOrCreateTagAsync(tagName);
                            EditingTrack.TrackTags.Add(new TrackTag 
                            { 
                                Track = EditingTrack,
                                Tag = tag
                            });
                        }

                        await _trackRepository.UpdateAsync(EditingTrack);
                        await RefreshTagsAsync();

                        IsEditTagsOpen = false;
                        EditingTrack = null;
                        EditingTags = "";
                        
                        TagsChanged?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating tags: {ex.Message}");
                    }
                }
            });

            CloseEditTagsCommand = new RelayCommand(() =>
            {
                IsEditTagsOpen = false;
                EditingTrack = null;
                EditingTags = "";
            });
        }

        public async Task RefreshTagsAsync()
        {
            try
            {
                var tags = await _tagRepository.GetAllTagsAsync();
                _availableTags.Clear();
                foreach (var tag in tags)
                {
                    _availableTags.Add(new TagViewModel
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        TrackCount = await _tagRepository.GetTrackCountForTagAsync(tag.Id)
                    });
                }
                this.RaisePropertyChanged(nameof(AvailableTags));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing tags: {ex.Message}");
            }
        }
    }
}
