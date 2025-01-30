using System;
using System.Threading.Tasks;
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
        public static IServiceProvider? ServiceProvider { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && ServiceProvider != null)
            {
                var dbContext = ServiceProvider.GetRequiredService<MusicPlayerDbContext>();
                var trackRepository = ServiceProvider.GetRequiredService<ITrackRepository>();
                var tagRepository = ServiceProvider.GetRequiredService<ITagRepository>();
                var audioPlayer = ServiceProvider.GetRequiredService<AudioPlayerService>();
                var sessionService = ServiceProvider.GetRequiredService<ISessionService>();

                var mainViewModel = new MainViewModel(
                    audioPlayer,
                    trackRepository,
                    tagRepository,
                    sessionService
                );

                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                desktop.Exit += async (s, e) =>
                {
                    mainViewModel?.Dispose();

                    if (ServiceProvider is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (ServiceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
