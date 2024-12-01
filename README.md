# TabletopTunes
A music player built with Avalonia UI that streams from YouTube URLs and organizes music with tags. Made specifically to catalogue and play mood music at the TTRPG table.

## Prerequisites

### Windows
No additional prerequisites needed.

### Linux
Install VLC and LibVLC development files:
Ubuntu/Debian:
```bash
sudo apt-get install vlc libvlc-dev
```
Fedora:
```bash
sudo dnf install vlc vlc-devel
```

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
- Linux: `~/.local/share/ModernMusicPlayer/musicplayer.db`
- Windows: `%LOCALAPPDATA%\ModernMusicPlayer\musicplayer.db`

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
