using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ModernMusicPlayer.Data;
using ModernMusicPlayer.Repositories;
using ModernMusicPlayer.Services;

namespace ModernMusicPlayer
{
    class Program
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CheckLinuxDependencies();
            }

            // Set up dependency injection
            var services = new ServiceCollection();
            
            // Register services
            services.AddDbContext<MusicPlayerDbContext>();
            services.AddScoped<ITrackRepository, TrackRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddSingleton<AudioPlayerService>();
            
            // Build the service provider
            ServiceProvider = services.BuildServiceProvider();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();

        private static void CheckLinuxDependencies()
        {
            // Common paths where libvlc might be installed
            string[] vlcPaths = new[]
            {
                "/usr/lib/libvlc.so",
                "/usr/lib/x86_64-linux-gnu/libvlc.so",
                "/usr/lib/i386-linux-gnu/libvlc.so",
                "/usr/local/lib/libvlc.so"
            };

            bool vlcFound = Array.Exists(vlcPaths, File.Exists);
            if (!vlcFound)
            {
                Console.WriteLine("VLC is not installed. Please install VLC and libvlc-dev packages.");
                Console.WriteLine("For Ubuntu/Debian: sudo apt-get install vlc libvlc-dev");
                Console.WriteLine("For Fedora: sudo dnf install vlc vlc-devel");
                Environment.Exit(1);
            }
        }
    }
}