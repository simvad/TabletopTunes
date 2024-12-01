using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using ReactiveUI;
using ModernMusicPlayer.Services;
using ModernMusicPlayer.Repositories;
using ModernMusicPlayer.Entities;
using ModernMusicPlayer.Commands;
using ModernMusicPlayer.Query;

namespace ModernMusicPlayer
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly AudioPlayerService _audioPlayer;
        private readonly ITrackRepository _trackRepository;
        private readonly ITagRepository _tagRepository;
        private readonly Random _random = new();
        private List<TrackEntity> _playQueue = new();
        
        // Master list of all tracks
        private ObservableCollection<TrackEntity> _allTracks;
        
        // Filtered view for display
        private ReadOnlyObservableCollection<TrackEntity> _displayedTracks;
        public ReadOnlyObservableCollection<TrackEntity> DisplayedTracks => _displayedTracks;

        // Available tags
        private ObservableCollection<TagViewModel> _availableTags;
        public ObservableCollection<TagViewModel> AvailableTags => _availableTags;

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

        private TrackEntity? _currentTrack;
        public TrackEntity? CurrentTrack
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

        private bool _isEditTagsOpen = false;
        public bool IsEditTagsOpen
        {
            get => _isEditTagsOpen;
            set => this.RaiseAndSetIfChanged(ref _isEditTagsOpen, value);
        }

        private bool _isRandomLoopActive;
        public bool IsRandomLoopActive
        {
            get => _isRandomLoopActive;
            private set => this.RaiseAndSetIfChanged(ref _isRandomLoopActive, value);
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

        public ICommand? OpenSettingsCommand { get; private set; }
        public ICommand? CloseSettingsCommand { get; private set; }
        public ICommand? OpenAddTrackCommand { get; private set; }
        public ICommand? CloseAddTrackCommand { get; private set; }
        public ICommand? PlayTrackCommand { get; private set; }
        public ICommand? AddTrackCommand { get; private set; }
        public ICommand? PlayPauseCommand { get; private set; }
        public ICommand? StopCommand { get; private set; }
        public ICommand? SeekCommand { get; private set; }
        public ICommand? VolumeCommand { get; private set; }
        public ICommand? EditTrackTagsCommand { get; private set; }
        public ICommand? DeleteTrackCommand { get; private set; }
        public ICommand? SaveTagsCommand { get; private set; }
        public ICommand? CloseEditTagsCommand { get; private set; }

        public MainViewModel(
            AudioPlayerService audioPlayer,
            ITrackRepository trackRepository,
            ITagRepository tagRepository)
        {
            _audioPlayer = audioPlayer;
            _trackRepository = trackRepository;
            _tagRepository = tagRepository;
            
            // Initialize collections
            _allTracks = new ObservableCollection<TrackEntity>();
            _availableTags = new ObservableCollection<TagViewModel>();
            _displayedTracks = new ReadOnlyObservableCollection<TrackEntity>(new ObservableCollection<TrackEntity>());

            // Load initial data
            _ = LoadDataAsync();

            _audioPlayer.PlaybackStarted += (s, e) => 
            {
                IsPlaying = true;
                Duration = _audioPlayer.Duration;
            };
            _audioPlayer.PlaybackFinished += async (s, e) => 
            {
                IsPlaying = false;
                if (IsRandomLoopActive)
                {
                    await Task.Delay(500); // Small delay to ensure clean transition
                    await PlayNextInQueue();
                }
            };
            _audioPlayer.PositionChanged += (s, position) => CurrentPosition = position;
            _audioPlayer.ErrorOccurred += (s, error) => 
            {
                Console.WriteLine($"Playback error: {error}");
            };

            InitializeCommands();
        }

        public async Task StartRandomPlayback()
        {
            var filteredTracks = DisplayedTracks.ToList();
            if (!filteredTracks.Any())
                return;

            _playQueue = filteredTracks.ToList();
            ShuffleQueue();
            IsRandomLoopActive = true;

            if (_playQueue.Any())
            {
                var firstTrack = _playQueue[0];
                _playQueue.RemoveAt(0);
                await PlayTrack(firstTrack);
            }
        }

        private void ShuffleQueue()
        {
            int n = _playQueue.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                TrackEntity temp = _playQueue[k];
                _playQueue[k] = _playQueue[n];
                _playQueue[n] = temp;
            }
        }

        private async Task PlayNextInQueue()
        {
            if (!_playQueue.Any() || !IsRandomLoopActive)
            {
                IsRandomLoopActive = false;
                return;
            }

            var nextTrack = _playQueue[0];
            _playQueue.RemoveAt(0);

            // If queue is empty, reshuffle all tracks
            if (!_playQueue.Any() && IsRandomLoopActive)
            {
                _playQueue = DisplayedTracks.ToList();
                ShuffleQueue();
            }

            await PlayTrack(nextTrack);
        }

        private async Task PlayTrack(TrackEntity track)
        {
            if (track?.Url != null)
            {
                CurrentTrack = track;
                try
                {
                    await _audioPlayer.PlayFromYoutubeUrl(track.Url);
                    
                    // Update play statistics in database
                    await _trackRepository.IncrementPlayCountAsync(track.Id);
                    await _trackRepository.UpdateLastPlayedAsync(track.Id);
                    
                    // Refresh the track data
                    var updatedTrack = await _trackRepository.GetByIdAsync(track.Id);
                    if (updatedTrack != null)
                    {
                        var index = _allTracks.IndexOf(track);
                        _allTracks[index] = updatedTrack;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing track: {ex.Message}");
                    if (IsRandomLoopActive)
                    {
                        await PlayNextInQueue(); // Skip to next track if current one fails
                    }
                }
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var tracks = await _trackRepository.GetAllAsync();
                _allTracks = new ObservableCollection<TrackEntity>(tracks);
                await RefreshTagsAsync();
                UpdateFilteredTracks();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private async Task RefreshTagsAsync()
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

        private void InitializeCommands()
        {
            OpenSettingsCommand = new RelayCommand(() => IsSettingsOpen = true);
            CloseSettingsCommand = new RelayCommand(() => IsSettingsOpen = false);
            OpenAddTrackCommand = new RelayCommand(() => IsAddTrackOpen = true);
            CloseAddTrackCommand = new RelayCommand(() => IsAddTrackOpen = false);

            EditTrackTagsCommand = new RelayCommand<TrackEntity>(track =>
            {
                EditingTrack = track;
                EditingTags = string.Join(", ", track?.TrackTags.Select(tt => tt.Tag.Name) ?? Array.Empty<string>());
                IsEditTagsOpen = true;
            });

            DeleteTrackCommand = new RelayCommand<TrackEntity>(async track =>
            {
                if (track != null)
                {
                    await _trackRepository.DeleteAsync(track.Id);
                    _allTracks.Remove(track);
                    UpdateFilteredTracks();
                }
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
                                Tag = tag,
                                AddedAt = DateTime.UtcNow
                            });
                        }

                        await _trackRepository.UpdateAsync(EditingTrack);
                        await RefreshTagsAsync();
                        UpdateFilteredTracks();

                        IsEditTagsOpen = false;
                        EditingTrack = null;
                        EditingTags = "";
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

            PlayTrackCommand = new RelayCommand<TrackEntity>(async track =>
            {
                if (track?.Url != null)
                {
                    // Stop any current random playback
                    IsRandomLoopActive = false;
                    _playQueue.Clear();
                    
                    await PlayTrack(track);
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

                        var tagNames = NewTrackTags.Split(',')
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t));

                        foreach (var tagName in tagNames)
                        {
                            var tag = await _tagRepository.GetOrCreateTagAsync(tagName);
                            newTrack.TrackTags.Add(new TrackTag 
                            { 
                                Track = newTrack,
                                Tag = tag,
                                AddedAt = DateTime.UtcNow
                            });
                        }

                        var savedTrack = await _trackRepository.AddAsync(newTrack);
                        _allTracks.Add(savedTrack);
                        await RefreshTagsAsync();
                        UpdateFilteredTracks();

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

            PlayPauseCommand = new RelayCommand(() =>
            {
                if (IsPlaying)
                    _audioPlayer.Pause();
                else
                    _audioPlayer.Resume();
            });

            StopCommand = new RelayCommand(() => 
            {
                _audioPlayer.Stop();
                IsRandomLoopActive = false;
                _playQueue.Clear();
            });

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
            IEnumerable<TrackEntity> filtered = _allTracks;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.AsQueryable().ApplyQuery(SearchQuery);
            }

            _displayedTracks = new ReadOnlyObservableCollection<TrackEntity>(
                new ObservableCollection<TrackEntity>(filtered)
            );
            
            this.RaisePropertyChanged(nameof(DisplayedTracks));

            // If we're in random loop mode and current track isn't in filtered set
            if (IsRandomLoopActive && CurrentTrack != null)
            {
                if (!filtered.Contains(CurrentTrack))
                {
                    // Stop current track and move to next track that matches new filter
                    _audioPlayer.Stop();
                    _playQueue = filtered.ToList();
                    ShuffleQueue();
                    _ = PlayNextInQueue();
                }
                else
                {
                    // Just update the queue with new filtered tracks
                    _playQueue = filtered.Where(t => t != CurrentTrack).ToList();
                    ShuffleQueue();
                }
            }
        }

        public void Dispose()
        {
            _audioPlayer?.Dispose();
        }
    }

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