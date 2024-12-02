using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ModernMusicPlayer.Commands;
using ModernMusicPlayer.Entities;
using ModernMusicPlayer.Repositories;
using ModernMusicPlayer.Services;
using ReactiveUI;

namespace ModernMusicPlayer.ViewModels
{
    public class TrackManagementViewModel : ReactiveObject
    {
        private readonly ITrackRepository _trackRepository;
        private readonly AudioPlayerService _audioPlayer;

        private ObservableCollection<TrackEntity> _allTracks = new();
        public ObservableCollection<TrackEntity> AllTracks => _allTracks;

        private bool _isAddTrackOpen;
        public bool IsAddTrackOpen
        {
            get => _isAddTrackOpen;
            set => this.RaiseAndSetIfChanged(ref _isAddTrackOpen, value);
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

        public ICommand? OpenAddTrackCommand { get; private set; }
        public ICommand? CloseAddTrackCommand { get; private set; }
        public ICommand? AddTrackCommand { get; private set; }
        public ICommand? DeleteTrackCommand { get; private set; }

        public event EventHandler? TracksChanged;

        public TrackManagementViewModel(
            ITrackRepository trackRepository,
            AudioPlayerService audioPlayer)
        {
            _trackRepository = trackRepository;
            _audioPlayer = audioPlayer;

            InitializeCommands();
            _ = LoadTracksAsync();
        }

        private async Task LoadTracksAsync()
        {
            try
            {
                var tracks = await _trackRepository.GetAllAsync();
                _allTracks = new ObservableCollection<TrackEntity>(tracks);
                TracksChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tracks: {ex.Message}");
            }
        }

        private void InitializeCommands()
        {
            OpenAddTrackCommand = new RelayCommand(() => IsAddTrackOpen = true);
            CloseAddTrackCommand = new RelayCommand(() => 
            {
                IsAddTrackOpen = false;
                NewTrackUrl = "";
                NewTrackTags = "";
            });

            DeleteTrackCommand = new RelayCommand<TrackEntity>(async track =>
            {
                if (track != null)
                {
                    await _trackRepository.DeleteAsync(track.Id);
                    _allTracks.Remove(track);
                    TracksChanged?.Invoke(this, EventArgs.Empty);
                }
            });

            AddTrackCommand = new RelayCommand(async () =>
            {
                if (!string.IsNullOrWhiteSpace(NewTrackUrl))
                {
                    try
                    {
                        var title = await _audioPlayer.GetVideoTitle(NewTrackUrl);
                        
                        var newTrack = new TrackEntity
                        {
                            Title = title,
                            Url = NewTrackUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        var savedTrack = await _trackRepository.AddAsync(newTrack);
                        _allTracks.Add(savedTrack);
                        TracksChanged?.Invoke(this, EventArgs.Empty);

                        IsAddTrackOpen = false;
                        NewTrackUrl = "";
                        NewTrackTags = "";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding track: {ex.Message}");
                    }
                }
            });
        }

        public async Task UpdateTrackStatistics(string trackId)
        {
            await _trackRepository.IncrementPlayCountAsync(trackId);
            await _trackRepository.UpdateLastPlayedAsync(trackId);
            
            // Refresh the track data
            var updatedTrack = await _trackRepository.GetByIdAsync(trackId);
            if (updatedTrack != null)
            {
                var existingTrack = _allTracks.FirstOrDefault(t => t.Id == trackId);
                if (existingTrack != null)
                {
                    var index = _allTracks.IndexOf(existingTrack);
                    _allTracks[index] = updatedTrack;
                    TracksChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
