﻿using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ModernMusicPlayer.Data;
using ModernMusicPlayer.Repositories;
using ModernMusicPlayer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Net;

namespace ModernMusicPlayer
{
    class Program
    {
        private static IHost? WebHost { get; set; }
        private static bool useLocalDevelopment = true;  // Set to false for Azure production

        [STAThread]
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CheckLinuxDependencies();
            }

            // Set up session configuration based on environment
            var sessionConfig = useLocalDevelopment
                ? SessionConfiguration.CreateLocalDevelopment()
                : SessionConfiguration.CreateAzureProduction("https://your-azure-url.com");  // Replace with your Azure URL

            // Start local web host for SignalR if needed (only in local development)
            if (sessionConfig.StartLocalServer)
            {
                Task.Run(() => StartWebHost());
            }

            // Create host builder
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddDbContext<MusicPlayerDbContext>();
                    services.AddScoped<ITrackRepository, TrackRepository>();
                    services.AddScoped<ITagRepository, TagRepository>();
                    services.AddSingleton<AudioPlayerService>();
                    services.AddSingleton(sessionConfig);  // Register configuration
                    services.AddSingleton<ISessionService, SessionService>();
                })
                .Build();

            // Set the service provider in App
            App.ServiceProvider = host.Services;

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            // Cleanup web host on application exit
            if (WebHost != null)
            {
                WebHost.StopAsync().Wait();
            }

            // Dispose the host when the application exits
            host.Dispose();
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
                socket.Close();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private static async Task StartWebHost()
        {
            // Check if port 5000 is available before attempting to start the server
            if (!IsPortAvailable(5000))
            {
                // Port is in use, likely by another instance. Skip server startup silently.
                return;
            }

            var builder = WebApplication.CreateBuilder();

            // Add services to the container
            builder.Services.AddSignalR();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("http://localhost:*")
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            try
            {
                WebHost = builder.Build();

                // Configure the HTTP request pipeline
                var app = (WebApplication)WebHost;

                app.UseCors();
                app.MapHub<SessionHub>("/sessionHub");

                await app.RunAsync("http://localhost:5000");
            }
            catch (IOException)
            {
                // If server startup fails (e.g., port became unavailable after our check),
                // handle it silently as the application can still function as a client
                WebHost = null;
            }
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
