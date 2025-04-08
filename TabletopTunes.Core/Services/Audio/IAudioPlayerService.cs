using System;
using System.Threading.Tasks;

namespace TabletopTunes.Core.Services.Audio
{
    public interface IAudioPlayerService : IDisposable
    {
        event EventHandler<EventArgs>? PlaybackFinished;
        event EventHandler<EventArgs>? PlaybackStarted;
        event EventHandler<TimeSpan>? PositionChanged;
        event EventHandler<string>? ErrorOccurred;
        event EventHandler<float>? BufferingProgress;

        bool IsPlaying { get; }
        bool IsBuffering { get; }
        TimeSpan Duration { get; }
        TimeSpan CurrentPosition { get; }

        Task PlayFromYoutubeUrl(string url);
        Task<string> GetVideoTitle(string url);
        void Pause();
        void Resume();
        void Stop();
        void SetVolume(int volume);
        void Seek(TimeSpan position);
    }
}