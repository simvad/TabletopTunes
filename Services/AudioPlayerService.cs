using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace ModernMusicPlayer.Services
{
    public class AudioPlayerService : IDisposable
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly YoutubeClient _youtube;
        private readonly Timer _positionTimer;
        private bool _isDisposed;

        public event EventHandler<EventArgs>? PlaybackFinished;
        public event EventHandler<EventArgs>? PlaybackStarted;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<string>? ErrorOccurred;

        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

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
                // Initialize LibVLC based on platform
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // On Linux, use system-installed VLC
                    _libVlc = new LibVLC();
                }
                else
                {
                    // On Windows or other platforms, use default initialization
                    Core.Initialize();
                    _libVlc = new LibVLC();
                }

                _mediaPlayer = new MediaPlayer(_libVlc);
                _youtube = new YoutubeClient();

                // Setup position update timer
                _positionTimer = new Timer(1000); // Update every second
                _positionTimer.Elapsed += (s, e) => 
                {
                    if (IsPlaying)
                    {
                        PositionChanged?.Invoke(this, CurrentPosition);
                    }
                };
                _positionTimer.Start();

                // Setup media player events
                _mediaPlayer.EndReached += (s, e) => PlaybackFinished?.Invoke(this, EventArgs.Empty);
                _mediaPlayer.Playing += (s, e) => PlaybackStarted?.Invoke(this, EventArgs.Empty);
                _mediaPlayer.EncounteredError += (s, e) => ErrorOccurred?.Invoke(this, "Playback error occurred");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to initialize audio player: {ex.Message}");
                throw;
            }
        }

        public async Task PlayFromYoutubeUrl(string url)
        {
            try
            {
                // Stop any current playback
                _mediaPlayer.Stop();

                // Get video ID and stream info
                var videoId = VideoId.Parse(url);
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams()
                    .OrderByDescending(stream => stream.Bitrate.BitsPerSecond)
                    .First();

                // Create and play media
                using var media = new Media(_libVlc, audioStreamInfo.Url, FromType.FromLocation);
                _mediaPlayer.Media = media;
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to play YouTube URL: {ex.Message}");
                throw;
            }
        }

        public void Pause()
        {
            if (_mediaPlayer.CanPause)
            {
                _mediaPlayer.Pause();
            }
        }

        public void Resume()
        {
            if (!IsPlaying)
            {
                _mediaPlayer.Play();
            }
        }

        public void Stop()
        {
            _mediaPlayer.Stop();
        }

        public void SetVolume(int volume)
        {
            _mediaPlayer.Volume = Math.Clamp(volume, 0, 100);
        }

        public void Seek(TimeSpan position)
        {
            if (_mediaPlayer.IsSeekable)
            {
                _mediaPlayer.Time = (long)position.TotalMilliseconds;
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