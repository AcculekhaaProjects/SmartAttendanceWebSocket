using Attendance.WS;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseWebSockets();

var socketHandler = new MyWebSocketHandler();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/pub/chat")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            //var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            //var hostName = Dns.GetHostEntry(context.Connection.RemoteIpAddress!).HostName;
            //DataLogger.SaveTextToLog($"New WebSocket connection from {remoteIp} ({hostName})");

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await socketHandler.HandleAsync(context, socket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next(context);
    }
});

app.MapGet("/", () => "Connected!");

app.Run();
