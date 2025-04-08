# TabletopTunes

A music player built with Avalonia UI that streams from YouTube URLs and organizes music with tags. Made specifically to catalogue and play mood music at the TTRPG table.

## Debugging
The app is developed on Linux but tested on Windows and should therefore work cross-platform, although errors might creep in on Windows. The most likely source of errors is a deprecated version of YoutubeExplode, since I'm not always on top of updating the package whenever YT makes a change.


## Project Structure

TabletopTunes is organized into three main components:

- **TabletopTunes.Core**: Contains domain models, repositories, and service interfaces
- **TabletopTunes.Server**: Hosts the SignalR hub for synchronized playback
- **TabletopTunes.App**: The desktop application with Avalonia UI

## Prerequisites

### Windows
Install VLC.

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

Note: If you're using PipeWire instead of PulseAudio, the PulseAudio compatibility layer shoul be used automatically.

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

## Deployment Options

TabletopTunes uses a flexible configuration system that supports different deployment scenarios:

### Configuration System

The application uses a robust configuration system with environment-specific settings:

- **appsettings.json**: Default configuration for all environments
- **appsettings.Development.json**: Settings for development environment
- **appsettings.Production.json**: Settings for production/deployed environment

You can configure server connections, database paths, and audio settings without modifying code.

### Server Connection Modes

TabletopTunes supports three modes for the session hub server:

#### 1. Local Development Mode
In development mode (default):
- Automatically starts a SignalR hub server on a dynamically selected port
- Handles all session management locally
- Perfect for testing and development on a single machine

#### 2. Remote Server Mode
For connecting to an existing server (cross-machine testing):
- Connect to a server on another machine on your network
- Use command-line arguments to specify the server URL:
  ```bash
  # Connect to a server on another machine
  dotnet run --project TabletopTunes.App -- --server "http://192.168.1.100:5000"
  ```
- Or modify the Server:Url setting in appsettings.json

#### 3. Production Deployment Mode
For production deployment:
- Deploy the server component separately
- Configure clients to connect to the deployed server
- When publishing the app as a single-file executable, it automatically uses production mode
- No local server processes are started in published mode

### Publishing for Windows

To create a standalone Windows executable:

```bash
# Make the script executable (first time only)
chmod +x publish-windows.sh

# Run the publish script
./publish-windows.sh
```

This creates a self-contained single-file executable at:
`TabletopTunes.App/bin/Release/net9.0/win-x64/publish/TabletopTunes.App.exe`

The published app:
- Includes all dependencies (no .NET installation required)
- Uses production configuration settings
- Does not attempt to start a local server
- Connects to the server URL specified in appsettings.Production.json
- Stores its database in the user's AppData folder

### Using Multiple Instances

To test with multiple instances:

1. Start one instance as the host:
   - Click the "Session" button
   - Click "Host Session"
   - Note the session code that appears

2. In other instances:
   - Click the "Session" button
   - Click "Join"
   - Enter the session code from step 1

All instances will now be synchronized, with the host controlling playback.
