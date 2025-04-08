@echo off
setlocal EnableDelayedExpansion

echo ====================================
echo Starting TabletopTunes Components...
echo ====================================

:: Check for existing server processes
echo Checking for existing server processes...
for /f "tokens=2" %%p in ('tasklist /fi "imagename eq dotnet.exe" /fo csv ^| findstr /i "TabletopTunes.Server"') do (
    echo Killing existing server process: %%p
    taskkill /F /PID %%p 2>nul
)

:: Start the server in the background
echo Starting TabletopTunes Server...
start /b cmd /c "dotnet run --project TabletopTunes.Server"

:: Wait for server to initialize
echo Waiting for server to initialize...
timeout /t 2 /nobreak > nul

:: Start the app
echo Starting TabletopTunes App...
dotnet run --project TabletopTunes.App
set APP_EXIT_CODE=%ERRORLEVEL%

:: When app is closed, terminate the server
echo App closed with exit code %APP_EXIT_CODE%
echo Cleaning up processes...

:: Kill any lingering server processes
echo Checking for lingering server processes...
for /f "tokens=2" %%p in ('tasklist /fi "imagename eq dotnet.exe" /fo csv ^| findstr /i "TabletopTunes.Server"') do (
    echo Killing lingering server process: %%p
    taskkill /F /PID %%p 2>nul
)

echo ====================================
echo TabletopTunes shutdown complete
echo ====================================

exit /b 0