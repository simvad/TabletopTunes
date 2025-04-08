using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean(_ => _
                .SetProject(Solution)
                .SetConfiguration(Configuration));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(_ => _
                .SetProjectFile(Solution.GetProject("TabletopTunes.Tests"))
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    Target RunApp => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(_ => _
                .SetProjectFile(RootDirectory / "TabletopTunes.App" / "TabletopTunes.App.csproj")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    Target RunServer => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(_ => _
                .SetProjectFile(RootDirectory / "TabletopTunes.Server" / "TabletopTunes.Server.csproj")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    Target RunAll => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // Kill any existing server processes (platform-independent)
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var processName = process.ProcessName.ToLower();
                    if (processName.Contains("dotnet") && 
                        ProcessTasks.StartProcess("ps", $"-p {process.Id} -o args", logOutput: false)
                            .AssertWaitForExit()
                            .Output
                            .Any(line => line.Contains("TabletopTunes.Server")))
                    {
                        Log.Information($"Killing existing server process: {process.Id}");
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                }
                catch (Exception ex) 
                {
                    Log.Warning($"Error checking process {process.Id}: {ex.Message}");
                }
            }

            // Start the server in a background process
            var serverProcess = ProcessTasks.StartProcess(
                DotNetPath,
                $"run --project {RootDirectory / "TabletopTunes.Server" / "TabletopTunes.Server.csproj"} --configuration {Configuration} --no-restore --no-build",
                RootDirectory,
                logOutput: true,
                logInvocation: true);

            Log.Information("Server starting. Waiting 2 seconds for initialization...");
            Thread.Sleep(2000);

            try
            {
                // Run the app in the foreground
                DotNetRun(_ => _
                    .SetProjectFile(RootDirectory / "TabletopTunes.App" / "TabletopTunes.App.csproj")
                    .SetConfiguration(Configuration) 
                    .EnableNoRestore()
                    .EnableNoBuild());
            }
            finally
            {
                // Clean up server process
                Log.Information("App closed. Shutting down server...");
                
                try 
                {
                    serverProcess.Kill();
                    serverProcess.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Error terminating server process: {ex.Message}");
                }

                // Check for any lingering processes
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        var processName = process.ProcessName.ToLower();
                        if (processName.Contains("dotnet") && 
                            ProcessTasks.StartProcess("ps", $"-p {process.Id} -o args", logOutput: false)
                                .AssertWaitForExit()
                                .Output
                                .Any(line => line.Contains("TabletopTunes.Server")))
                        {
                            Log.Information($"Killing lingering server process: {process.Id}");
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                    }
                    catch (Exception ex) 
                    {
                        Log.Warning($"Error checking process {process.Id}: {ex.Message}");
                    }
                }
            }
        });

    Target ApplyMigrations => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            ProcessTasks.StartProcess(
                "dotnet", 
                $"ef database update --project {RootDirectory / "TabletopTunes.Core" / "TabletopTunes.Core.csproj"}", 
                RootDirectory,
                logOutput: true)
                .AssertZeroExitCode();
        });
}