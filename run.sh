#!/bin/bash

# Trap to ensure proper cleanup on exit
cleanup() {
    echo "Cleaning up processes..."
    
    # Kill server if it exists
    if [ -n "$SERVER_PID" ] && ps -p $SERVER_PID > /dev/null; then
        echo "Killing server process ($SERVER_PID)..."
        kill -9 $SERVER_PID
        wait $SERVER_PID 2>/dev/null
    fi
    
    # Kill any other lingering processes - both direct executable and dotnet-hosted
    echo "Checking for lingering server processes..."
    
    # Find TabletopTunes.Server executable processes
    EXECUTABLE_PIDS=$(ps aux | grep "TabletopTunes.Server" | grep -v grep | awk '{print $2}')
    
    # Find dotnet processes running TabletopTunes.Server
    DOTNET_PIDS=$(ps aux | grep "dotnet" | grep "TabletopTunes.Server" | grep -v grep | awk '{print $2}')
    
    # Combine both lists
    ALL_SERVER_PIDS="$EXECUTABLE_PIDS $DOTNET_PIDS"
    
    if [ -n "$ALL_SERVER_PIDS" ]; then
        echo "Killing ALL server processes: $ALL_SERVER_PIDS"
        for PID in $ALL_SERVER_PIDS; do
            echo "Killing process $PID..."
            kill -9 $PID 2>/dev/null
        done
    else
        echo "No lingering server processes found"
    fi
    
    echo "===================================="
    echo "TabletopTunes shutdown complete"
    echo "===================================="
    exit 0
}

# Set up trap for various signals
trap cleanup SIGINT SIGTERM EXIT

# Print a header
echo "===================================="
echo "Starting TabletopTunes Components..."
echo "===================================="

# Check for and kill any existing server processes
echo "Checking for existing server processes..."
EXISTING_PIDS=$(ps aux | grep "dotnet" | grep "TabletopTunes.Server" | grep -v grep | awk '{print $2}')
if [ -n "$EXISTING_PIDS" ]; then
    echo "Killing existing server processes: $EXISTING_PIDS"
    kill $EXISTING_PIDS 2>/dev/null
    sleep 1
fi

# Start the server in the background
echo "Starting TabletopTunes Server..."
dotnet run --project TabletopTunes.Server &
SERVER_PID=$!

# Wait a moment for the server to initialize
echo "Waiting for server to initialize..."
sleep 2

# Start the app
echo "Starting TabletopTunes App..."
dotnet run --project TabletopTunes.App
APP_EXIT_CODE=$?

# When app is closed, terminate the server (this will also trigger the cleanup trap)
echo "App closed with exit code $APP_EXIT_CODE"