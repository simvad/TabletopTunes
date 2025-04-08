# TabletopTunes

A music player built with Avalonia UI that streams from YouTube URLs and organizes music with tags. Made specifically to catalogue and play mood music at the TTRPG table.

## Project Structure

TabletopTunes is organized into three main components:

- **TabletopTunes.Core**: Contains domain models, repositories, and service interfaces
- **TabletopTunes.Server**: Hosts the SignalR hub for synchronized playback
- **TabletopTunes.App**: The desktop application with Avalonia UI

## Prerequisites

### Windows
No additional prerequisites needed.

### Linux
Install VLC, LibVLC development files, and PulseAudio:

Ubuntu/Debian:
```bash
sudo apt-get install vlc libvlc-dev pulseaudio
```

Fedora:
```bash
sudo dnf install vlc vlc-devel pulseaudio
```

Note: If you're using PipeWire instead of PulseAudio, the PulseAudio compatibility layer will be used automatically.

## Setup

1. Install .NET 8.0 SDK
2. Clone the repository
3. Install Entity Framework Core tools:
```bash
dotnet tool install --global dotnet-ef
```
4. Create the database:
```bash
cd TabletopTunes.Core
dotnet ef database update
```

The database will be created in:
- Linux: `~/.local/share/TabletopTunes/musicplayer.db`
- Windows: `%LOCALAPPDATA%\TabletopTunes\musicplayer.db`

## Running the Application

The simplest way to run TabletopTunes is using the provided scripts:

### Windows
```batch
# Run both server and app
run.bat
```

### Linux/macOS
```bash
# Make the script executable (first time only)
chmod +x run.sh

# Run both server and app
./run.sh
```

Both scripts perform the same functions but are optimized for their respective operating systems. They'll start the server in the background, wait for it to initialize, then launch the app. When you close the app, they'll automatically clean up the server process.

Alternatively, you can run the components individually:

```bash
# Start the SignalR server first
dotnet run --project TabletopTunes.Server

# Then in another terminal, start the app
dotnet run --project TabletopTunes.App
```

## Session Hub Configuration

TabletopTunes supports two modes for the session hub server that enables synchronized playback:

### Local Development Mode
By default, the application runs in local development mode, which:
- Automatically starts a SignalR hub server on localhost:5000
- Handles all session management locally
- Perfect for testing and development

When you run the application in local mode:
1. The SignalR hub server starts on port 5000
2. The main application window opens
3. Multiple instances can be run for testing

To test with multiple instances locally:
1. Run the server once: `dotnet run --project TabletopTunes.Server`
2. Open additional terminals and run more app instances:
   ```bash
   dotnet run --project TabletopTunes.App
   ```
3. In one instance:
   - Click the "Session" button
   - Click "Host Session"
   - Note the session code that appears
4. In other instances:
   - Click the "Session" button
   - Click "Join"
   - Enter the session code from step 3

All instances will now be synchronized, with the host controlling playback.

#### Audio Separation on Linux
When running multiple instances on Linux, TabletopTunes automatically handles audio separation:
- Host instance uses the system's default audio output
- Guest instances use dedicated virtual audio sinks
- Each instance can have independent volume control
- Virtual sinks are automatically created and cleaned up
- Works with both PulseAudio and PipeWire

This means you can test synchronized playback on a single machine without audio conflicts. Each instance's volume can be controlled independently through the system's audio settings or the application's volume control.

### Azure Deployment Mode
For production deployment, the session hub can be hosted on Azure:
1. Deploy the SignalR hub to Azure
2. Modify `Program.cs` in the App project to use the Azure configuration:
```csharp
// Change this line in Program.cs
var sessionConfig = SessionConfiguration.CreateAzureProduction("https://your-azure-url.com");
```

When you run the app in Azure mode:
1. The application connects directly to your Azure-hosted hub
2. Multiple instances across different machines can connect to the same Azure hub
3. All session management is handled by the Azure service
