using TabletopTunes.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:*")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.MapHub<SessionHub>("/sessionHub");

// Allow command line port configuration
int port = 5000;
if (args.Length > 0 && int.TryParse(args[0], out int customPort))
{
    port = customPort;
}

// Check if the port is already in use
bool isPortAvailable = true;
try
{
    var socket = new System.Net.Sockets.Socket(
        System.Net.Sockets.AddressFamily.InterNetwork,
        System.Net.Sockets.SocketType.Stream,
        System.Net.Sockets.ProtocolType.Tcp);
    socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), port));
    socket.Close();
}
catch (System.Net.Sockets.SocketException)
{
    isPortAvailable = false;
    Console.WriteLine($"Port {port} is already in use.");

    // Find an available port
    for (int p = 5001; p < 5100; p++)
    {
        try
        {
            var socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp);
            socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), p));
            socket.Close();
            port = p;
            isPortAvailable = true;
            Console.WriteLine($"Using alternate port {port}");
            break;
        }
        catch (System.Net.Sockets.SocketException)
        {
            continue;
        }
    }
}

if (isPortAvailable)
{
    app.Run($"http://localhost:{port}");
}
else
{
    Console.WriteLine("Failed to find available port. Exiting.");
    return;
}