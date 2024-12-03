using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ModernMusicPlayer.Commands;
using ModernMusicPlayer.Entities;
using ModernMusicPlayer.Services;
using ReactiveUI;
using System.Reactive.Linq;

namespace ModernMusicPlayer.ViewModels
{
    public class PlaybackViewModel : ReactiveObject, IDisposable
    {
        private readonly AudioPlayerService _audioPlayer;
        private readonly ISessionService _sessionService;
        private readonly Random _random = new();
        private List<TrackEntity> _playQueue = new();
        private bool _isUpdatingFromSession;
        private IDisposable? _playbackStateSubscription;
        private IDisposable? _trackChangedSubscription;
        private IDisposable? _sessionEndedSubscription;
        private System.Timers.Timer? _syncTimer;
        private DateTime _lastStateUpdate = DateTime.UtcNow;
        private int _currentVolume = 100; // Store current volume level

        private TrackEntity? _currentTrack;
        public TrackEntity? CurrentTrack
        {
            get => _currentTrack;
            set => this.RaiseAndSetIfChanged(ref _currentTrack, value);
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            private set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
        }

        private bool _isRandomLoopActive;
        public bool IsRandomLoopActive
        {
            get => _isRandomLoopActive;
            private set => this.RaiseAndSetIfChanged(ref _isRandomLoopActive, value);
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

        private double _fadeLength = 2.0;
        public double FadeLength
        {
            get => _fadeLength;
            set => this.RaiseAndSetIfChanged(ref _fadeLength, value);
        }

        public ICommand? PlayPauseCommand { get; private set; }
        public ICommand? StopCommand { get; private set; }
        public ICommand? SeekCommand { get; private set; }
        public ICommand? VolumeCommand { get; private set; }
        public ICommand? PlayTrackCommand { get; private set; }

        public event EventHandler<TrackEntity>? TrackPlayed;

        public PlaybackViewModel(AudioPlayerService audioPlayer, ISessionService sessionService)
        {
            _audioPlayer = audioPlayer;
            _sessionService = sessionService;

            InitializeAudioPlayerEvents();
            InitializeSessionEvents();
            InitializeCommands();
            InitializeSyncTimer();

            // Set initial volume
            _audioPlayer.SetVolume(_currentVolume);
        }

        private void InitializeSyncTimer()
        {
            if (!_sessionService.IsHost)
            {
                _syncTimer = new System.Timers.Timer(5000); // Check every 5 seconds
                _syncTimer.Elapsed += async (s, e) =>
                {
                    if (_sessionService.IsConnected && !_isUpdatingFromSession && 
                        (DateTime.UtcNow - _lastStateUpdate).TotalSeconds > 5)
                    {
                        await _sessionService.RequestSyncState();
                    }
                };
                _syncTimer.Start();
            }
        }

        private void InitializeSessionEvents()
        {
            _playbackStateSubscription = _sessionService.PlaybackStateChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(state =>
                {
                    if (!_sessionService.IsHost)
                    {
                        _isUpdatingFromSession = true;
                        _lastStateUpdate = DateTime.UtcNow;

                        if (state.IsPlaying != IsPlaying)
                        {
                            if (state.IsPlaying)
                            {
                                _audioPlayer.Resume();
                                // Restore volume after resume
                                _audioPlayer.SetVolume(_currentVolume);
                            }
                            else
                                _audioPlayer.Pause();
                            IsPlaying = state.IsPlaying;
                        }

                        // Only seek if the difference is significant (more than 1 second)
                        var currentPos = _audioPlayer.CurrentPosition;
                        var timeDiff = Math.Abs((state.Position - currentPos).TotalSeconds);
                        if (timeDiff > 1)
                        {
                            _audioPlayer.Seek(state.Position);
                            CurrentPosition = state.Position;
                        }

                        _isUpdatingFromSession = false;
                    }
                });

            _trackChangedSubscription = _sessionService.TrackChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(trackId =>
                {
                    if (!_sessionService.IsHost)
                    {
                        var track = _playQueue.FirstOrDefault(t => t.Id.ToString() == trackId);
                        if (track != null)
                        {
                            _isUpdatingFromSession = true;
                            _ = PlayTrack(track);
                            _isUpdatingFromSession = false;
                        }
                    }
                });

            _sessionEndedSubscription = _sessionService.SessionEnded
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    _isUpdatingFromSession = false;
                    _syncTimer?.Stop();
                });
        }

        private void InitializeAudioPlayerEvents()
        {
            _audioPlayer.PlaybackStarted += async (s, e) => 
            {
                IsPlaying = true;
                Duration = _audioPlayer.Duration;
                // Restore volume when playback starts
                _audioPlayer.SetVolume(_currentVolume);

                if (_sessionService.IsHost && !_isUpdatingFromSession)
                {
                    await _sessionService.UpdatePlaybackState(true, TimeSpan.Zero);
                }
            };
            
            _audioPlayer.PlaybackFinished += async (s, e) => 
            {
                IsPlaying = false;
                if (_sessionService.IsHost)
                {
                    await _sessionService.UpdatePlaybackState(false, TimeSpan.Zero);
                }
                
                if (IsRandomLoopActive && _sessionService.IsHost)
                {
                    await Task.Delay(500);
                    await PlayNextInQueue();
                }
            };
            
            _audioPlayer.PositionChanged += async (s, position) => 
            {
                CurrentPosition = position;
                if (_sessionService.IsHost && !_isUpdatingFromSession)
                {
                    // Only send position updates every second to reduce network traffic
                    if ((DateTime.UtcNow - _lastStateUpdate).TotalSeconds >= 1)
                    {
                        await _sessionService.UpdatePlaybackState(IsPlaying, position);
                        _lastStateUpdate = DateTime.UtcNow;
                    }
                }
            };
            
            _audioPlayer.ErrorOccurred += (s, error) => 
            {
                Console.WriteLine($"Playback error: {error}");
            };
        }

        private void InitializeCommands()
        {
            PlayTrackCommand = new RelayCommand<TrackEntity>(async track =>
            {
                if (track?.Url != null && (_sessionService.IsHost || !_sessionService.IsConnected))
                {
                    IsRandomLoopActive = false;
                    _playQueue.Clear();
                    await PlayTrack(track);
                }
            });

            PlayPauseCommand = new RelayCommand(async () =>
            {
                if (_sessionService.IsConnected && !_sessionService.IsHost)
                    return;

                if (IsPlaying)
                {
                    _audioPlayer.Pause();
                    IsPlaying = false;
                }
                else
                {
                    _audioPlayer.Resume();
                    // Restore volume after resume
                    _audioPlayer.SetVolume(_currentVolume);
                    IsPlaying = true;
                }

                if (_sessionService.IsHost)
                {
                    await _sessionService.UpdatePlaybackState(IsPlaying, CurrentPosition);
                    _lastStateUpdate = DateTime.UtcNow;
                }
            });

            StopCommand = new RelayCommand(async () => 
            {
                if (_sessionService.IsConnected && !_sessionService.IsHost)
                    return;

                _audioPlayer.Stop();
                IsPlaying = false;
                IsRandomLoopActive = false;
                _playQueue.Clear();

                if (_sessionService.IsHost)
                {
                    await _sessionService.UpdatePlaybackState(false, TimeSpan.Zero);
                    _lastStateUpdate = DateTime.UtcNow;
                }
            });

            SeekCommand = new RelayCommand<double>(async position =>
            {
                if (_sessionService.IsConnected && !_sessionService.IsHost)
                    return;

                var seekPosition = TimeSpan.FromSeconds(position);
                _audioPlayer.Seek(seekPosition);

                if (_sessionService.IsHost)
                {
                    await _sessionService.UpdatePlaybackState(IsPlaying, seekPosition);
                    _lastStateUpdate = DateTime.UtcNow;
                }
            });

            VolumeCommand = new RelayCommand<double>(volume =>
            {
                _currentVolume = (int)volume;
                _audioPlayer.SetVolume(_currentVolume);
            });
        }

        public async Task PlayTrack(TrackEntity track)
        {
            if (track?.Url != null)
            {
                CurrentTrack = track;
                try
                {
                    await _audioPlayer.PlayFromYoutubeUrl(track.Url);
                    // Restore volume after starting new track
                    _audioPlayer.SetVolume(_currentVolume);
                    TrackPlayed?.Invoke(this, track);

                    if (_sessionService.IsHost && !_isUpdatingFromSession)
                    {
                        await _sessionService.UpdateTrack(track.Id.ToString());
                        await _sessionService.UpdatePlaybackState(true, TimeSpan.Zero);
                        _lastStateUpdate = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing track: {ex.Message}");
                    if (IsRandomLoopActive && _sessionService.IsHost)
                    {
                        await PlayNextInQueue();
                    }
                }
            }
        }

        public async Task StartRandomPlayback(IEnumerable<TrackEntity> tracks)
        {
            if (!_sessionService.IsHost && _sessionService.IsConnected)
                return;

            var tracksList = new List<TrackEntity>(tracks);
            if (!tracksList.Any())
                return;

            _playQueue = tracksList;
            ShuffleQueue();
            IsRandomLoopActive = true;

            if (_playQueue.Any())
            {
                var firstTrack = _playQueue[0];
                _playQueue.RemoveAt(0);
                await PlayTrack(firstTrack);
            }
        }

        public void UpdatePlayQueue(IEnumerable<TrackEntity> tracks)
        {
            if (CurrentTrack != null)
            {
                _playQueue = new List<TrackEntity>(tracks.Where(t => t != CurrentTrack));
            }
            else
            {
                _playQueue = new List<TrackEntity>(tracks);
            }
            ShuffleQueue();
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
            await PlayTrack(nextTrack);
        }

        public void Dispose()
        {
            _audioPlayer?.Dispose();
            _playbackStateSubscription?.Dispose();
            _trackChangedSubscription?.Dispose();
            _sessionEndedSubscription?.Dispose();
            _syncTimer?.Dispose();
        }
    }
}
