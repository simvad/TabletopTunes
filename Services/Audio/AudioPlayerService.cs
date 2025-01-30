using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace ModernMusicPlayer.Services
{
    public class AudioPlayerService : IAudioPlayerService
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly YouTubePlaybackService _youtubeService;
        private readonly AudioPlayerEventHandler _eventHandler;
        private Media? _currentMedia;
        private bool _isDisposed;
        private readonly bool _isHost;

        public event EventHandler<EventArgs>? PlaybackFinished
        {
            add => _eventHandler.PlaybackFinished += value;
            remove => _eventHandler.PlaybackFinished -= value;
        }

        public event EventHandler<EventArgs>? PlaybackStarted
        {
            add => _eventHandler.PlaybackStarted += value;
            remove => _eventHandler.PlaybackStarted -= value;
        }

        public event EventHandler<TimeSpan>? PositionChanged
        {
            add => _eventHandler.PositionChanged += value;
            remove => _eventHandler.PositionChanged -= value;
        }

        public event EventHandler<string>? ErrorOccurred
        {
            add => _eventHandler.ErrorOccurred += value;
            remove => _eventHandler.ErrorOccurred -= value;
        }

        public event EventHandler<float>? BufferingProgress
        {
            add => _eventHandler.BufferingProgress += value;
            remove => _eventHandler.BufferingProgress -= value;
        }

        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;
        public bool IsBuffering => _eventHandler.IsBuffering;

        public TimeSpan Duration => _mediaPlayer?.Length is long length && length != -1
            ? TimeSpan.FromMilliseconds(length)
            : TimeSpan.Zero;

        public TimeSpan CurrentPosition => _mediaPlayer?.Time is long time
            ? TimeSpan.FromMilliseconds(time)
            : TimeSpan.Zero;

        public AudioPlayerService(bool isHost = true)
        {
            _isHost = isHost;
            try
            {
                // Create LibVLC instance with host/guest specific configuration
                _libVlc = AudioPlayerConfiguration.CreateLibVLC(isHost);
                _mediaPlayer = new MediaPlayer(_libVlc);
                _eventHandler = new AudioPlayerEventHandler(_mediaPlayer);
                _youtubeService = new YouTubePlaybackService(_libVlc, 
                    error => _eventHandler.RaiseError(error));

                // Set initial volume based on host/guest status
                _mediaPlayer.Volume = 100;
            }
            catch (Exception ex)
            {
                _eventHandler?.RaiseError($"Failed to initialize audio player: {ex.Message}");
                throw;
            }
        }

        public async Task PlayFromYoutubeUrl(string url)
        {
            try
            {
                Stop();

                _currentMedia = await _youtubeService.CreateMediaFromUrl(url);
                _mediaPlayer.Media = _currentMedia;
                _mediaPlayer.Play();

                await _eventHandler.WaitForPlaybackStart(_currentMedia);
            }
            catch (Exception ex)
            {
                _eventHandler.RaiseError($"Failed to play YouTube URL: {ex.Message}");
                throw;
            }
        }

        public Task<string> GetVideoTitle(string url)
        {
            return _youtubeService.GetVideoTitle(url);
        }

        public void Pause()
        {
            if (_mediaPlayer?.CanPause == true)
            {
                _mediaPlayer.Pause();
            }
        }

        public void Resume()
        {
            if (_mediaPlayer != null && !IsPlaying)
            {
                _mediaPlayer.Play();
            }
        }

        public void Stop()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
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
                    _eventHandler.Dispose();
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
