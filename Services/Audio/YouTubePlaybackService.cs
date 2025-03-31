using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using System.Net;

namespace TabletopTunes.Services
{
    internal class YouTubePlaybackService
    {
        private readonly YoutubeClient _youtube;
        private readonly LibVLC _libVlc;
        private readonly Action<string> _onError;
        private readonly HttpClient _httpClient;

        public YouTubePlaybackService(LibVLC libVlc, Action<string> onError)
        {
            var cookieContainer = new CookieContainer();
            // Add required cookies for YouTube
            cookieContainer.Add(new Cookie("CONSENT", "YES+", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("VISITOR_INFO1_LIVE", "random_value", "/", "youtube.com"));

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler);
            // Add required headers to appear as a normal browser request
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            _httpClient.DefaultRequestHeaders.Add("DNT", "1");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            
            _youtube = new YoutubeClient(_httpClient);
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
                IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams()
                    .OrderByDescending(s => s.Bitrate.BitsPerSecond)
                    .FirstOrDefault() as IStreamInfo
                    ?? streamManifest.GetMuxedStreams()
                        .OrderByDescending(s => s.VideoQuality)
                        .FirstOrDefault() as IStreamInfo
                    ?? throw new Exception("No suitable stream found");

                var media = new Media(_libVlc, streamInfo.Url, FromType.FromLocation);
                AudioPlayerConfiguration.ConfigureMedia(media);
                return media;
            }
            catch (YoutubeExplode.Exceptions.VideoUnplayableException ex)
            {
                var message = $"Video is unplayable. This may be due to age restrictions or requiring sign-in. Error: {ex.Message}";
                _onError(message);
                throw new Exception(message, ex);
            }
            catch (Exception ex)
            {
                var message = $"Failed to create media from YouTube URL: {ex.Message}";
                _onError(message);
                throw new Exception(message, ex);
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
