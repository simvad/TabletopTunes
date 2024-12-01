using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using YoutubeExplode;
using YoutubeExplode.Videos;
using System.Collections.Generic;

namespace ModernMusicPlayer.Services
{
    public class AudioPlayerService : IDisposable
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly YoutubeClient _youtube;
        private readonly Timer _positionTimer;
        private Media? _currentMedia;
        private bool _isDisposed;
        private bool _isPlayingState;
        private const int BufferSize = 32768; // 32KB buffer

        public event EventHandler<EventArgs>? PlaybackFinished;
        public event EventHandler<EventArgs>? PlaybackStarted;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<float>? BufferingProgress;

        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;
        public bool IsBuffering { get; private set; }

        public TimeSpan Duration => _mediaPlayer?.Length is long length && length != -1
            ? TimeSpan.FromMilliseconds(length)
            : TimeSpan.Zero;

        public TimeSpan CurrentPosition => _mediaPlayer?.Time is long time
            ? TimeSpan.FromMilliseconds(time)
            : TimeSpan.Zero;

        public AudioPlayerService()
        {
            try
            {
                // Configure LibVLC with platform-specific settings
                var options = new List<string>
                {
                    "--network-caching=3000",         // Increase network cache to 3 seconds
                    "--live-caching=1500",            // Live stream caching
                    "--sout-mux-caching=1500",        // Muxer caching
                    "--file-caching=1500",            // File caching
                    "--http-reconnect",               // Enable HTTP reconnection
                    "--no-video",                     // Disable video decoding
                    $"--file-caching={BufferSize}",   // File caching buffer size
                    "--audio-resampler=soxr",         // High quality audio resampler
                    "--clock-jitter=0",               // Minimize clock jitter
                    "--clock-synchro=0"               // Disable clock synchro for smoother playback
                };

                // Add platform-specific options
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    options.Add("--aout=mmdevice");   // Use modern audio output on Windows only
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // On Linux, use system-installed VLC
                    _libVlc = new LibVLC(options.ToArray());
                }
                else
                {
                    // On Windows or other platforms, use default initialization
                    Core.Initialize();
                    _libVlc = new LibVLC(options.ToArray());
                }

                _mediaPlayer = new MediaPlayer(_libVlc);
                _youtube = new YoutubeClient();

                // Enhanced position update timer
                _positionTimer = new Timer(500); // Update twice per second
                _positionTimer.Elapsed += (s, e) =>
                {
                    if (IsPlaying && !IsBuffering)
                    {
                        PositionChanged?.Invoke(this, CurrentPosition);
                    }
                };
                _positionTimer.Start();

                // Enhanced event handling
                SetupMediaPlayerEvents();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to initialize audio player: {ex.Message}");
                throw;
            }
        }

        private void SetupMediaPlayerEvents()
        {
            _mediaPlayer.EndReached += (s, e) => PlaybackFinished?.Invoke(this, EventArgs.Empty);
            _mediaPlayer.Playing += (s, e) => 
            {
                IsBuffering = false;
                _isPlayingState = true;
                PlaybackStarted?.Invoke(this, EventArgs.Empty);
            };
            _mediaPlayer.Buffering += (s, e) =>
            {
                IsBuffering = e.Cache < 100.0f;
                BufferingProgress?.Invoke(this, e.Cache);
            };
            _mediaPlayer.EncounteredError += (s, e) =>
            {
                _isPlayingState = false;
                string error = "Playback error occurred";
                ErrorOccurred?.Invoke(this, error);
                
                // Auto-retry on certain errors
                Task.Delay(1000).ContinueWith(_ => RetryPlayback());
            };
        }

        private async Task RetryPlayback()
        {
            if (_currentMedia != null)
            {
                try
                {
                    await PlayMedia(_currentMedia);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"Retry failed: {ex.Message}");
                }
            }
        }

        private async Task PlayMedia(Media media)
        {
            _currentMedia = media;
            
            // Configure media for optimal streaming
            media.AddOption(":network-caching=3000");
            media.AddOption(":clock-jitter=0");
            media.AddOption(":clock-synchro=0");

            _mediaPlayer.Media = media;
            _mediaPlayer.Play();

            // Wait for media to start or error
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<EventArgs>? playingHandler = null;
            EventHandler<EventArgs>? errorHandler = null;

            playingHandler = (s, e) =>
            {
                _mediaPlayer.Playing -= playingHandler;
                _mediaPlayer.EncounteredError -= errorHandler;
                tcs.TrySetResult(true);
            };

            errorHandler = (s, e) =>
            {
                _mediaPlayer.Playing -= playingHandler;
                _mediaPlayer.EncounteredError -= errorHandler;
                tcs.TrySetException(new Exception("Media playback error occurred"));
            };

            _mediaPlayer.Playing += playingHandler;
            _mediaPlayer.EncounteredError += errorHandler;

            // Wait for playback to start with timeout
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task;
            }
        }

        public async Task PlayFromYoutubeUrl(string url)
        {
            try
            {
                _mediaPlayer.Stop();

                var videoId = VideoId.Parse(url);
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);
                
                // Get best audio stream with fallback options
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams()
                    .OrderByDescending(s => s.Bitrate.BitsPerSecond)
                    .FirstOrDefault()
                    ?? throw new Exception("No suitable audio stream found");

                using var media = new Media(_libVlc, audioStreamInfo.Url, FromType.FromLocation);
                await PlayMedia(media);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to play YouTube URL: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetVideoTitle(string url)
        {
            try
            {
                var videoId = VideoId.Parse(url);
                var video = await _youtube.Videos.GetAsync(videoId);
                return video.Title;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to get video title: {ex.Message}");
                return "Unknown Title";
            }
        }

        public void Pause()
        {
            if (_mediaPlayer?.CanPause == true)
            {
                _mediaPlayer.Pause();
                _isPlayingState = false;
            }
        }

        public void Resume()
        {
            if (_mediaPlayer != null && !_isPlayingState)
            {
                _mediaPlayer.Play();
                _isPlayingState = true;
            }
        }

        public void Stop()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _isPlayingState = false;
                _currentMedia?.Dispose();
                _currentMedia = null;
            }
        }

        public void SetVolume(int volume)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Clamp(volume, 0, 100);
            }
        }

        public void Seek(TimeSpan position)
        {
            if (_mediaPlayer?.IsSeekable == true && !IsBuffering)
            {
                // Implement smooth seeking
                var targetMs = (long)position.TotalMilliseconds;
                if (Math.Abs(targetMs - _mediaPlayer.Time) > 1000)
                {
                    _mediaPlayer.Time = targetMs;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _positionTimer.Stop();
                    _positionTimer.Dispose();
                    _mediaPlayer.Stop();
                    _mediaPlayer.Dispose();
                    _currentMedia?.Dispose();
                    _libVlc.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AudioPlayerService()
        {
            Dispose(false);
        }
    }
}