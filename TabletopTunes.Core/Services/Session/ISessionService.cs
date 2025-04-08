using System;
using System.Threading.Tasks;

namespace TabletopTunes.Core.Services.Session
{
    public interface ISessionService
    {
        bool IsConnected { get; }
        bool IsHost { get; }
        string? SessionCode { get; }
        
        Task<bool> StartHosting();
        Task<bool> JoinSession(string code);
        Task LeaveSession();
        Task UpdatePlaybackState(bool isPlaying, TimeSpan position);
        Task UpdateTrack(string trackId);
        Task RequestSyncState();

        IObservable<(bool IsPlaying, TimeSpan Position)> PlaybackStateChanged { get; }
        IObservable<string> TrackChanged { get; }
        IObservable<string> ClientJoined { get; }
        IObservable<string> ClientLeft { get; }
        IObservable<Unit> SessionEnded { get; }
    }

    public readonly struct Unit
    {
        public static readonly Unit Default = new();
    }
}