using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using ModernMusicPlayer.Services;
using System.Threading.Tasks;

namespace ModernMusicPlayer
{
    public class Track
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "Untitled Track";
        public string Url { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string AudioPath { get; set; } = string.Empty;
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter) =>
            parameter is T typedParameter && (_canExecute?.Invoke(typedParameter) ?? true);

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                _execute(typedParameter);
            }
        }
    }

    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly AudioPlayerService _audioPlayer;
        
        // Master list of all tracks
        private ObservableCollection<Track> _allTracks;
        
        // Filtered view for display
        private ReadOnlyObservableCollection<Track> _displayedTracks;
        public ReadOnlyObservableCollection<Track> DisplayedTracks => _displayedTracks;

        private string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchQuery, value);
                UpdateFilteredTracks();
            }
        }

        private Track? _currentTrack;
        public Track? CurrentTrack
        {
            get => _currentTrack;
            set => this.RaiseAndSetIfChanged(ref _currentTrack, value);
        }

        private double _fadeLength = 2.0;
        public double FadeLength
        {
            get => _fadeLength;
            set => this.RaiseAndSetIfChanged(ref _fadeLength, value);
        }

        private bool _isSettingsOpen = false;
        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set => this.RaiseAndSetIfChanged(ref _isSettingsOpen, value);
        }

        private bool _isAddTrackOpen = false;
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

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            private set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
        }

        private TimeSpan _currentPosition;
        public TimeSpan CurrentPosition
        {
            get => _currentPosition;
            private set => this.RaiseAndSetIfChanged(ref _currentPosition, value);
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get => _duration;
            private set => this.RaiseAndSetIfChanged(ref _duration, value);
        }

public ICommand OpenSettingsCommand { get; private set; } = null!;
public ICommand CloseSettingsCommand { get; private set; } = null!;
public ICommand OpenAddTrackCommand { get; private set; } = null!;
public ICommand CloseAddTrackCommand { get; private set; } = null!;
public ICommand PlayTrackCommand { get; private set; } = null!;
public ICommand AddTrackCommand { get; private set; } = null!;
public ICommand PlayPauseCommand { get; private set; } = null!;
public ICommand StopCommand { get; private set; } = null!;
public ICommand SeekCommand { get; private set; } = null!;
public ICommand VolumeCommand { get; private set; } = null!;

        public MainViewModel()
        {
            _audioPlayer = new AudioPlayerService();
            
            // Initialize master list
            _allTracks = new ObservableCollection<Track>
            {
                new Track { Title = "Sample Track 1", Tags = new List<string> { "rock", "alternative" } },
                new Track { Title = "Sample Track 2", Tags = new List<string> { "electronic", "dance" } }
            };

            // Initial filter setup
            _displayedTracks = new ReadOnlyObservableCollection<Track>(new ObservableCollection<Track>(_allTracks));

            // Wire up audio player events
            _audioPlayer.PlaybackStarted += (s, e) => 
            {
                IsPlaying = true;
                Duration = _audioPlayer.Duration;
            };
            _audioPlayer.PlaybackFinished += (s, e) => IsPlaying = false;
            _audioPlayer.PositionChanged += (s, position) => CurrentPosition = position;
            _audioPlayer.ErrorOccurred += (s, error) => 
            {
                // TODO: Show error in UI
                Console.WriteLine($"Playback error: {error}");
            };

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            OpenSettingsCommand = new RelayCommand(() => IsSettingsOpen = true);
            CloseSettingsCommand = new RelayCommand(() => IsSettingsOpen = false);
            OpenAddTrackCommand = new RelayCommand(() => IsAddTrackOpen = true);
            CloseAddTrackCommand = new RelayCommand(() => IsAddTrackOpen = false);

            PlayTrackCommand = new RelayCommand<Track>(async track =>
            {
                if (track?.Url != null)
                {
                    CurrentTrack = track;
                    try
                    {
                        await _audioPlayer.PlayFromYoutubeUrl(track.Url);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error playing track: {ex.Message}");
                    }
                }
            });

            AddTrackCommand = new RelayCommand(async () =>
            {
                if (!string.IsNullOrWhiteSpace(NewTrackUrl))
                {
                    try
                    {
                        var title = await _audioPlayer.GetVideoTitle(NewTrackUrl);
                        var newTrack = new Track
                        {
                            Title = title,
                            Url = NewTrackUrl,
                            Tags = NewTrackTags.Split(',')
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrEmpty(t))
                                .ToList()
                        };

                        _allTracks.Add(newTrack);  // Add to master list
                        IsAddTrackOpen = false;
                        NewTrackUrl = "";
                        NewTrackTags = "";
                        UpdateFilteredTracks();  // Refresh the filter
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding track: {ex.Message}");
                    }
                }
            });

            PlayPauseCommand = new RelayCommand(() =>
            {
                if (IsPlaying)
                    _audioPlayer.Pause();
                else
                    _audioPlayer.Resume();
            });

            StopCommand = new RelayCommand(() => _audioPlayer.Stop());

            SeekCommand = new RelayCommand<double>(position =>
            {
                _audioPlayer.Seek(TimeSpan.FromSeconds(position));
            });

            VolumeCommand = new RelayCommand<double>(volume =>
            {
                _audioPlayer.SetVolume((int)volume);
            });
        }

        private void UpdateFilteredTracks()
        {
            IEnumerable<Track> filtered = _allTracks;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                // For now, simple contains search. You can enhance this later with logical operators
                filtered = _allTracks.Where(track =>
                    track.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    track.Tags.Any(tag => tag.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                );
            }

            // Create a new ReadOnlyObservableCollection for the filtered results
            _displayedTracks = new ReadOnlyObservableCollection<Track>(
                new ObservableCollection<Track>(filtered)
            );
            
            this.RaisePropertyChanged(nameof(DisplayedTracks));
        }

        public void Dispose()
        {
            _audioPlayer?.Dispose();
        }
    }
}