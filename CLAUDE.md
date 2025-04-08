# TabletopTunes Development Guide

## Build & Run Commands
- Run complete application: `./run.sh`
- Run server only: `dotnet run --project TabletopTunes.Server`
- Run app only: `dotnet run --project TabletopTunes.App`
- Build solution: `dotnet build`
- Run all tests: `dotnet test TabletopTunes.Tests`
- Run single test: `dotnet test TabletopTunes.Tests --filter "FullyQualifiedName=TabletopTunes.Tests.TrackQueryParserTests.ParseQuery_EmptyQuery_ReturnsAllTracks"`
- Apply database migrations: `cd TabletopTunes.Core && dotnet ef database update`

## Code Style Guidelines
- **Naming:** Classes/Properties/Methods: `PascalCase`, Parameters/Variables: `camelCase`, Interfaces: `IServiceName`, Private fields: `_camelCase`
- **Organization:** System imports first, then third-party, then project namespaces
- **Architecture:** MVVM pattern with ViewModels extending `ViewModelBase`
- **Commands:** Implement using `RelayCommand` pattern
- **Errors:** Use events for error propagation, ensure proper resource disposal
- **Tests:** xUnit with pattern: `[Method]_[Condition]_[ExpectedResult]`
- **Nulls:** Enable nullable reference types, use `?` suffix for nullable types
- **Structure:** Follow repository pattern for data access, service interfaces in Core project

## Project Architecture
- **Core:** Domain models, repositories, service interfaces
- **App:** Avalonia UI desktop application with MVVM pattern
- **Server:** SignalR hub for synchronized playback
- **Tests:** xUnit test project with AAA pattern