using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ModernMusicPlayer.Common;
using ModernMusicPlayer.Entities;
using ModernMusicPlayer.Repositories;
using ModernMusicPlayer.Services;
using ReactiveUI;

namespace ModernMusicPlayer.ViewModels
{
    public class TrackManagementViewModel : ReactiveObject
    {
        private readonly ITrackRepository _trackRepository;
        private readonly ITagRepository _tagRepository;
        private readonly AudioPlayerService _audioPlayer;

        private ObservableCollection<TrackEntity> _allTracks = new();
        public ObservableCollection<TrackEntity> AllTracks
        {
            get => _allTracks;
            private set => this.RaiseAndSetIfChanged(ref _allTracks, value);
        }

        private string _newTrackUrl = "";
        public string NewTrackUrl
        {
            get => _newTrackUrl;
            set => this.RaiseAndSetIfChanged(ref _newTrackUrl, value);
        }

        private string _newTrackTags = "";
        public string NewTrackTags
        {
            get => _newTrackTags;
            set => this.RaiseAndSetIfChanged(ref _newTrackTags, value);
        }

        public ICommand? AddTrackCommand { get; private set; }
        public ICommand? DeleteTrackCommand { get; private set; }

        public event EventHandler? TracksChanged;
        public event EventHandler? AddTrackCompleted;

        public TrackManagementViewModel(
            ITrackRepository trackRepository,
            ITagRepository tagRepository,
            AudioPlayerService audioPlayer)
        {
            _trackRepository = trackRepository;
            _tagRepository = tagRepository;
            _audioPlayer = audioPlayer;

            InitializeCommands();
            _ = LoadTracksAsync();
        }

        private async Task LoadTracksAsync()
        {
            try
            {
                var tracks = await _trackRepository.GetAllAsync();
                
                AllTracks.Clear();
                foreach (var track in tracks)
                {
                    AllTracks.Add(track);
                }
                
                TracksChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tracks: {ex.Message}");
            }
        }

        private void InitializeCommands()
        {
            DeleteTrackCommand = new RelayCommand<TrackEntity>(async track =>
            {
                if (track != null)
                {
                    await _trackRepository.DeleteAsync(track.Id);
                    AllTracks.Remove(track);
                    TracksChanged?.Invoke(this, EventArgs.Empty);
                }
            });

            AddTrackCommand = new RelayCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewTrackUrl))
                {
                    return;
                }

                try
                {
                    var title = await _audioPlayer.GetVideoTitle(NewTrackUrl);
                    
                    var newTrack = new TrackEntity
                    {
                        Title = title,
                        Url = NewTrackUrl,
                        TrackTags = new System.Collections.Generic.List<TrackTag>()
                    };

                    var savedTrack = await _trackRepository.AddAsync(newTrack);

                    if (!string.IsNullOrWhiteSpace(NewTrackTags))
                    {
                        var tagNames = NewTrackTags.Split(',')
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrWhiteSpace(t));

                        foreach (var tagName in tagNames)
                        {
                            var tag = await _tagRepository.GetOrCreateTagAsync(tagName);
                            
                            savedTrack.TrackTags.Add(new TrackTag
                            {
                                TrackId = savedTrack.Id,
                                TagId = tag.Id,
                                Tag = tag
                            });
                        }

                        await _trackRepository.UpdateAsync(savedTrack);
                    }

                    AllTracks.Add(savedTrack);
                    TracksChanged?.Invoke(this, EventArgs.Empty);

                    NewTrackUrl = "";
                    NewTrackTags = "";

                    AddTrackCompleted?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding track: {ex.Message}");
                }
            });
        }
    }
}
