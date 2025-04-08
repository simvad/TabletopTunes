using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using TabletopTunes.App.Services.Audio;
using TabletopTunes.App.Services.Session;
using TabletopTunes.Core.Data;
using TabletopTunes.Core.Repositories;
using TabletopTunes.Core.Services.Session;
using Avalonia.ReactiveUI; // Add this import

namespace TabletopTunes.App
{
    class Program
    {
        private static Process? _serverProcess;
        private static bool _useLocalDevelopment = true;  // Set to false for Azure production
        private static bool _cleanupPerformed = false;  // Flag to track if we've already run cleanup
        
        // Make this method public so it can be called from App.xaml.cs
        public static void StopAllServers()
        {
            if (!_cleanupPerformed)
            {
                _cleanupPerformed = true;
                StopServer();
            }
            else
            {
                Console.WriteLine("Server cleanup already performed, skipping redundant cleanup");
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            // Set up application exit handler
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => 
            {
                if (!_cleanupPerformed)
                {
                    _cleanupPerformed = true;
                    StopServer();
                }
            };
            
            Console.CancelKeyPress += (sender, e) => 
            {
                e.Cancel = true; // Prevent the process from terminating immediately
                if (!_cleanupPerformed)
                {
                    _cleanupPerformed = true;
                    StopServer();
                }
            };
            
            // Start local server if needed
            if (_useLocalDevelopment)
            {
                StartServer();
            }

            // Configure services
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<MusicPlayerDbContext>();
                    services.AddScoped<ITrackRepository, TrackRepository>();
                    services.AddScoped<ITagRepository, TagRepository>();
                    services.AddSingleton<AudioPlayerService>();
                    
                    // Configure session service
                    var sessionConfig = _useLocalDevelopment
                        ? SessionConfiguration.CreateLocalDevelopment()
                        : SessionConfiguration.CreateAzureProduction("https://your-azure-url.com");
                    
                    services.AddSingleton(sessionConfig);
                    services.AddSingleton<ISessionService, SessionService>();
                    
                    // Register MainViewModel
                    services.AddSingleton<ViewModels.MainViewModel>();
                })
                .Build();

            App.ServiceProvider = host.Services;
            
            // Initialize database
            try 
            {
                using var scope = host.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MusicPlayerDbContext>();
                Console.WriteLine("Initializing database...");
                dbContext.EnsureDatabaseCreated();
                Console.WriteLine("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                // Continue application execution even if DB setup fails
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            // Cleanup server when app exits
            StopServer();
            host.Dispose();
        }

        private static void StartServer()
        {
            try
            {
                // Find an available port
                int port = FindAvailablePort(5000, 5100);
                if (port == 0)
                {
                    Console.WriteLine("Failed to find available port. Skipping server startup.");
                    return;
                }

                Console.WriteLine($"Found available port: {port}");

                string serverPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..",
                    "TabletopTunes.Server", "bin", "Debug", "net9.0",
                    "TabletopTunes.Server.dll");

                _serverProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"\"{serverPath}\" {port}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _serverProcess.Start();

                // Give server time to start
                Task.Delay(1000).Wait();
                Console.WriteLine($"Session server started on port {port}");
                
                // Update session configuration with the dynamic port
                if (App.ServiceProvider != null)
                {
                    var sessionConfig = App.ServiceProvider.GetService<SessionConfiguration>();
                    if (sessionConfig != null)
                    {
                        sessionConfig.HubUrl = $"http://localhost:{port}/sessionHub";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start server: {ex.Message}");
            }
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
        
        private static int FindAvailablePort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return 0; // No available port found
        }

        private static void StopServer()
        {
            try
            {
                Console.WriteLine("Stopping server process...");
                
                // First, kill the process we started in this session (if any)
                if (_serverProcess != null)
                {
                    try
                    {
                        // Check if process is still available and running
                        if (!_serverProcess.HasExited)
                        {
                            _serverProcess.Kill(entireProcessTree: true);
                            Console.WriteLine("Session server stopped");
                        }
                        else
                        {
                            Console.WriteLine("Session server already exited");
                        }
                        
                        // Cleanup resources
                        _serverProcess.Dispose();
                        _serverProcess = null;
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Process was already disposed or unavailable");
                        _serverProcess = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error stopping main server process: {ex.Message}");
                        _serverProcess = null;
                    }
                }
                else
                {
                    Console.WriteLine("No server process was started in this session");
                }
                
                // Then, attempt to find and kill ALL TabletopTunes.Server processes
                // This includes both our process and any orphaned ones
                CleanupAllServerProcesses();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during server cleanup: {ex.Message}");
            }
        }
        
        private static void CleanupAllServerProcesses()
        {
            try
            {
                // Find all TabletopTunes.Server executable processes
                var serverExeProcesses = Process.GetProcessesByName("TabletopTunes.Server")
                    .ToList();
                
                // Find all dotnet processes running TabletopTunes.Server
                var dotnetServerProcesses = Process.GetProcessesByName("dotnet")
                    .Where(p => 
                    {
                        try
                        {
                            string? cmdLine = GetCommandLine(p.Id);
                            return cmdLine != null && cmdLine.Contains("TabletopTunes.Server");
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();
                
                // Combine both lists
                var allServerProcesses = serverExeProcesses.Concat(dotnetServerProcesses).ToList();
                
                if (allServerProcesses.Count > 0)
                {
                    Console.WriteLine($"Found {allServerProcesses.Count} server processes to clean up");
                    foreach (var process in allServerProcesses)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill(entireProcessTree: true);
                                Console.WriteLine($"Killed server process {process.Id}");
                            }
                            else
                            {
                                Console.WriteLine($"Process {process.Id} already exited");
                            }
                            
                            // Always clean up
                            process.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No server processes found to clean up");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during server process cleanup: {ex.Message}");
            }
        }
        
        private static void CleanupOrphanedServerProcesses()
        {
            try
            {
                // Find all dotnet processes that might be running TabletopTunes.Server
                var serverProcesses = Process.GetProcessesByName("dotnet")
                    .Where(p => 
                    {
                        try
                        {
                            string? cmdLine = GetCommandLine(p.Id);
                            return cmdLine != null && cmdLine.Contains("TabletopTunes.Server");
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();
                
                if (serverProcesses.Count() > 0)
                {
                    Console.WriteLine($"Found {serverProcesses.Count()} orphaned server processes to clean up");
                    foreach (var process in serverProcesses)
                    {
                        try
                        {
                            process.Kill();
                            process.Dispose();
                            Console.WriteLine($"Killed orphaned server process {process.Id}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during orphaned process cleanup: {ex.Message}");
            }
        }
        
        private static string? GetCommandLine(int processId)
        {
            // This works on Linux to get command line arguments
            try
            {
                return File.ReadAllText($"/proc/{processId}/cmdline").Replace('\0', ' ');
            }
            catch
            {
                return null;
            }
        }

        // The existing BuildAvaloniaApp method
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}