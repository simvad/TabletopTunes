using System;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace ModernMusicPlayer.Services
{
    internal class YouTubePlaybackService
    {
        private readonly YoutubeClient _youtube;
        private readonly LibVLC _libVlc;
        private readonly Action<string> _onError;

        public YouTubePlaybackService(LibVLC libVlc, Action<string> onError)
        {
            _youtube = new YoutubeClient();
            _libVlc = libVlc;
            _onError = onError;
        }

        public async Task<Media> CreateMediaFromUrl(string url)
        {
            try
            {
                var videoId = VideoId.Parse(url);
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId);
                
                // Get best audio stream with fallback options
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams()
                    .OrderByDescending(s => s.Bitrate.BitsPerSecond)
                    .FirstOrDefault()
                    ?? throw new Exception("No suitable audio stream found");

                var media = new Media(_libVlc, audioStreamInfo.Url, FromType.FromLocation);
                AudioPlayerConfiguration.ConfigureMedia(media);
                return media;
            }
            catch (Exception ex)
            {
                _onError($"Failed to create media from YouTube URL: {ex.Message}");
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
                _onError($"Failed to get video title: {ex.Message}");
                return "Unknown Title";
            }
        }
    }
}
