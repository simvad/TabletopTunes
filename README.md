# TabletopTunes
A music player built with Avalonia UI that streams from YouTube URLs and organizes music with tags. Made specifically to catalogue and play mood music at the TTRPG table.

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

1. Install .NET 7.0 SDK or later
2. Clone the repository
3. Install Entity Framework Core tools:
```bash
dotnet tool install --global dotnet-ef
```
4. Create the database:
```bash
dotnet ef database update
```

The database will be created in:
- Linux: `~/.local/share/TabletopTunes/musicplayer.db`
- Windows: `%LOCALAPPDATA%\TabletopTunes\musicplayer.db`

## Session Hub Configuration

TabletopTunes supports two modes for the session hub server that enables synchronized playback:

### Local Development Mode
By default, the application runs in local development mode, which:
- Automatically starts a SignalR hub server on localhost:5000
- Handles all session management locally
- Perfect for testing and development

When you run `dotnet run` in local mode:
1. The application starts a local SignalR hub server on port 5000
2. The main application window opens
3. Multiple instances can be run for testing

To test with multiple instances locally:
1. Open a terminal and run the first instance:
   ```bash
   dotnet run
   ```
   This instance will start the SignalR hub server and the application.

2. Open additional terminals and run more instances:
   ```bash
   dotnet run
   ```
   Each new instance will connect to the hub server started by the first instance.

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
2. Modify `Program.cs` to use the Azure configuration:
```csharp
// Change this line in Program.cs
var sessionConfig = SessionConfiguration.CreateAzureProduction("https://your-azure-url.com");
```

When you run `dotnet run` in Azure mode:
1. The application starts without launching a local server
2. The main application window opens
3. The application connects directly to your Azure-hosted hub
4. Multiple instances across different machines can connect to the same Azure hub
5. All session management is handled by the Azure service

Benefits of Azure deployment:
- Improved reliability and scalability
- No need to run a local server
- Better for production use
- Enables synchronization across different networks

Note: In Azure mode, make sure your Azure SignalR hub is running and accessible before starting the application.

## Features
- Stream music from YouTube URLs
- Organize tracks with tags
- Search by title or tags
- Tag-based filtering with operators:
  - Simple text search: "rock" (finds tracks with "rock" in title or tags)
  - Tag search: "#rock" (only searches tags)
  - Combined tags: "#rock & #alternative" (both tags must exist)
  - OR operations: "#rock | #jazz" (either tag)
  - NOT operations: "#rock & !#jazz" (rock but not jazz)
- Synchronized playback across multiple instances using session hub
  - Host a session and share the code
  - Join existing sessions
  - Synchronized play/pause and track changes
  - Independent volume control per instance
  - Automatic audio separation on Linux for local testing
