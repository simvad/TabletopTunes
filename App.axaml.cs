using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ModernMusicPlayer.Data;
using ModernMusicPlayer.Repositories;
using ModernMusicPlayer.Services;
using ModernMusicPlayer.ViewModels;

namespace ModernMusicPlayer
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && Program.ServiceProvider != null)
            {
                // Get required services
                var dbContext = Program.ServiceProvider.GetRequiredService<MusicPlayerDbContext>();
                var trackRepository = Program.ServiceProvider.GetRequiredService<ITrackRepository>();
                var tagRepository = Program.ServiceProvider.GetRequiredService<ITagRepository>();
                var audioPlayer = Program.ServiceProvider.GetRequiredService<AudioPlayerService>();
                var sessionService = Program.ServiceProvider.GetRequiredService<ISessionService>();

                // Create main view model with dependencies
                var mainViewModel = new MainViewModel(
                    audioPlayer,
                    trackRepository,
                    tagRepository,
                    sessionService
                );

                // Create and configure main window
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // Handle application shutdown
                desktop.Exit += (s, e) =>
                {
                    mainViewModel?.Dispose();
                    if (Program.ServiceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
