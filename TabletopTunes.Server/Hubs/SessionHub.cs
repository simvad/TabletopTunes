using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TabletopTunes.Core.Services.Session;

namespace TabletopTunes.Server.Hubs
{
    public class SessionHub : Hub
    {
        private static readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
        private static readonly ConcurrentDictionary<string, string> _clientSessions = new(); // connectionId -> sessionCode

        public async Task<string> CreateSession()
        {
            var sessionCode = GenerateSessionCode();
            var sessionInfo = new SessionInfo
            {
                HostConnectionId = Context.ConnectionId,
                CurrentTrackId = null,
                IsPlaying = false,
                Position = TimeSpan.Zero,
                LastUpdateTime = DateTime.UtcNow
            };

            _sessions[sessionCode] = sessionInfo;
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);
            return sessionCode;
        }

        public async Task<bool> JoinSession(string sessionCode)
        {
            if (!_sessions.TryGetValue(sessionCode, out var sessionInfo))
            {
                return false;
            }

            _clientSessions[Context.ConnectionId] = sessionCode;
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);
            
            // Notify host of new client
            await Clients.Client(sessionInfo.HostConnectionId).SendAsync("ClientJoined", Context.ConnectionId);
            
            // Send current track state to new client
            if (sessionInfo.CurrentTrackId != null)
            {
                await Clients.Caller.SendAsync("TrackChanged", sessionInfo.CurrentTrackId);
                await Clients.Caller.SendAsync("PlaybackStateChanged", sessionInfo.IsPlaying, sessionInfo.Position);
            }

            return true;
        }

        public async Task UpdatePlaybackState(string sessionCode, bool isPlaying, TimeSpan position)
        {
            if (_sessions.TryGetValue(sessionCode, out var sessionInfo) && 
                sessionInfo.HostConnectionId == Context.ConnectionId)
            {
                sessionInfo.IsPlaying = isPlaying;
                sessionInfo.Position = position;
                sessionInfo.LastUpdateTime = DateTime.UtcNow;
                await Clients.OthersInGroup(sessionCode).SendAsync("PlaybackStateChanged", isPlaying, position);
            }
        }

        public async Task UpdateTrack(string sessionCode, string trackId)
        {
            if (_sessions.TryGetValue(sessionCode, out var sessionInfo) && 
                sessionInfo.HostConnectionId == Context.ConnectionId)
            {
                sessionInfo.CurrentTrackId = trackId;
                sessionInfo.Position = TimeSpan.Zero;
                sessionInfo.LastUpdateTime = DateTime.UtcNow;
                await Clients.OthersInGroup(sessionCode).SendAsync("TrackChanged", trackId);
            }
        }

        public async Task RequestSyncState(string sessionCode)
        {
            if (_sessions.TryGetValue(sessionCode, out var sessionInfo))
            {
                // Only sync if we haven't received an update in the last 5 seconds
                if ((DateTime.UtcNow - sessionInfo.LastUpdateTime).TotalSeconds > 5)
                {
                    await Clients.Client(sessionInfo.HostConnectionId).SendAsync("RequestPlaybackState", Context.ConnectionId);
                }
            }
        }

        public async Task SendSyncState(string clientId, bool isPlaying, TimeSpan position)
        {
            await Clients.Client(clientId).SendAsync("PlaybackStateChanged", isPlaying, position);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_clientSessions.TryRemove(Context.ConnectionId, out string? sessionCode))
            {
                if (_sessions.TryGetValue(sessionCode, out var sessionInfo))
                {
                    // If host disconnects, end the session
                    if (sessionInfo.HostConnectionId == Context.ConnectionId)
                    {
                        _sessions.TryRemove(sessionCode, out _);
                        await Clients.OthersInGroup(sessionCode).SendAsync("SessionEnded");
                    }
                    else
                    {
                        // If client disconnects, notify host
                        await Clients.Client(sessionInfo.HostConnectionId).SendAsync("ClientLeft", Context.ConnectionId);
                    }
                }
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private static string GenerateSessionCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new char[6];

            do
            {
                for (int i = 0; i < code.Length; i++)
                {
                    code[i] = chars[random.Next(chars.Length)];
                }
            } while (_sessions.ContainsKey(new string(code)));

            return new string(code);
        }

        private class SessionInfo
        {
            public required string HostConnectionId { get; init; }
            public string? CurrentTrackId { get; set; }
            public bool IsPlaying { get; set; }
            public TimeSpan Position { get; set; }
            public DateTime LastUpdateTime { get; set; }
        }
    }
}