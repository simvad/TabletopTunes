using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Reactive.Subjects;

namespace ModernMusicPlayer.Services
{
    public class SessionService : ISessionService, IAsyncDisposable
    {
        private readonly SessionConfiguration _configuration;
        private HubConnection? _hubConnection;
        private string? _sessionCode;
        private bool _isHost;

        private readonly Subject<(bool IsPlaying, TimeSpan Position)> _playbackStateChanged = new();
        private readonly Subject<string> _trackChanged = new();
        private readonly Subject<string> _clientJoined = new();
        private readonly Subject<string> _clientLeft = new();
        private readonly Subject<Unit> _sessionEnded = new();

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public bool IsHost => _isHost;
        public string? SessionCode => _sessionCode;

        public IObservable<(bool IsPlaying, TimeSpan Position)> PlaybackStateChanged => _playbackStateChanged;
        public IObservable<string> TrackChanged => _trackChanged;
        public IObservable<string> ClientJoined => _clientJoined;
        public IObservable<string> ClientLeft => _clientLeft;
        public IObservable<Unit> SessionEnded => _sessionEnded;

        public SessionService(SessionConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task EnsureConnected()
        {
            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_configuration.HubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<bool, TimeSpan>("PlaybackStateChanged", (isPlaying, position) =>
                {
                    _playbackStateChanged.OnNext((isPlaying, position));
                });

                _hubConnection.On<string>("TrackChanged", trackId =>
                {
                    _trackChanged.OnNext(trackId);
                });

                _hubConnection.On<string>("ClientJoined", clientId =>
                {
                    _clientJoined.OnNext(clientId);
                });

                _hubConnection.On<string>("ClientLeft", clientId =>
                {
                    _clientLeft.OnNext(clientId);
                });

                _hubConnection.On("SessionEnded", () =>
                {
                    _sessionEnded.OnNext(Unit.Default);
                    _sessionCode = null;
                    _isHost = false;
                });

                _hubConnection.On<string>("RequestPlaybackState", async (clientId) =>
                {
                    if (_isHost)
                    {
                        await _hubConnection.InvokeAsync("SendSyncState", clientId, true, TimeSpan.Zero);
                    }
                });

                await _hubConnection.StartAsync();
            }
        }

        public async Task<bool> StartHosting()
        {
            try
            {
                await EnsureConnected();
                if (_hubConnection == null) return false;

                _sessionCode = await _hubConnection.InvokeAsync<string>("CreateSession");
                _isHost = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> JoinSession(string code)
        {
            try
            {
                await EnsureConnected();
                if (_hubConnection == null) return false;

                var success = await _hubConnection.InvokeAsync<bool>("JoinSession", code);
                if (success)
                {
                    _sessionCode = code;
                    _isHost = false;
                }
                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task LeaveSession()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            _sessionCode = null;
            _isHost = false;
        }

        public async Task UpdatePlaybackState(bool isPlaying, TimeSpan position)
        {
            if (_isHost && _hubConnection != null && _sessionCode != null)
            {
                await _hubConnection.InvokeAsync("UpdatePlaybackState", _sessionCode, isPlaying, position);
            }
        }

        public async Task UpdateTrack(string trackId)
        {
            if (_isHost && _hubConnection != null && _sessionCode != null)
            {
                await _hubConnection.InvokeAsync("UpdateTrack", _sessionCode, trackId);
            }
        }

        public async Task RequestSyncState()
        {
            if (!_isHost && _hubConnection != null && _sessionCode != null)
            {
                await _hubConnection.InvokeAsync("RequestSyncState", _sessionCode);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            _playbackStateChanged.Dispose();
            _trackChanged.Dispose();
            _clientJoined.Dispose();
            _clientLeft.Dispose();
            _sessionEnded.Dispose();
        }
    }

    public readonly struct Unit
    {
        public static readonly Unit Default = new();
    }
}
