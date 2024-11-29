using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.ReactiveUI;

namespace ModernMusicPlayer
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CheckLinuxDependencies();
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
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