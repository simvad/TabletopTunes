using System;
using System.Threading.Tasks;
using System.Timers;
using LibVLCSharp.Shared;

namespace ModernMusicPlayer.Services
{
    internal class AudioPlayerEventHandler
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly Timer _positionTimer;
        private bool _isPlayingState;

        public event EventHandler<EventArgs>? PlaybackFinished;
        public event EventHandler<EventArgs>? PlaybackStarted;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<float>? BufferingProgress;

        public bool IsBuffering { get; private set; }

        public AudioPlayerEventHandler(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
            
            // Enhanced position update timer
            _positionTimer = new Timer(500); // Update twice per second
            _positionTimer.Elapsed += (s, e) =>
            {
                if (_mediaPlayer.IsPlaying && !IsBuffering)
                {
                    PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(_mediaPlayer.Time));
                }
            };
            _positionTimer.Start();

            SetupMediaPlayerEvents();
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
            };
        }

        public void RaiseError(string error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        public async Task WaitForPlaybackStart(Media media, int timeoutSeconds = 10)
        {
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

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task;
            }
        }

        public void Dispose()
        {
            _positionTimer.Stop();
            _positionTimer.Dispose();
        }
    }
}
