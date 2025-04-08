using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TabletopTunes.App.ViewModels;
using TabletopTunes.App.Views;

namespace TabletopTunes.App
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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Create main window
                var mainWindow = new MainWindow();
                
                // ONLY register for application exit event - this happens once
                desktop.Exit += (_, _) => { 
                    try { 
                        Console.WriteLine("Application exiting, cleaning up server process...");
                        Program.StopAllServers(); 
                    } 
                    catch (Exception ex) { 
                        Console.WriteLine($"Error during exit cleanup: {ex.Message}"); 
                    } 
                };
                
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
        
        // MainWindow_Closing handler removed - we just use the Exit event
    }
}