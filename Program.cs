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

namespace ModernMusicPlayer
{
    class Program
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        private static IHost? WebHost { get; set; }

        [STAThread]
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CheckLinuxDependencies();
            }

            // Set up session configuration
            var sessionConfig = SessionConfiguration.CreateLocalDevelopment();
            // For production, use:
            // var sessionConfig = SessionConfiguration.CreateAzureProduction("https://your-azure-url.com");

            // Start local web host for SignalR if needed
            if (sessionConfig.StartLocalServer)
            {
                Task.Run(() => StartWebHost());
            }

            // Set up dependency injection
            var services = new ServiceCollection();
            
            // Register services
            services.AddDbContext<MusicPlayerDbContext>();
            services.AddScoped<ITrackRepository, TrackRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddSingleton<AudioPlayerService>();
            services.AddSingleton(sessionConfig);  // Register configuration
            services.AddSingleton<ISessionService, SessionService>();
            
            // Build the service provider
            ServiceProvider = services.BuildServiceProvider();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            // Cleanup web host on application exit
            if (WebHost != null)
            {
                WebHost.StopAsync().Wait();
            }
        }

        private static async Task StartWebHost()
        {
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

            WebHost = builder.Build();

            // Configure the HTTP request pipeline
            var app = (WebApplication)WebHost;

            app.UseCors();
            app.MapHub<SessionHub>("/sessionHub");

            await app.RunAsync("http://localhost:5000");
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
