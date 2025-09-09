using Attendance.BusinessLogic.Interfaces;
using Attendance.WS;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddWebSocketDI();
builder.Services.AddControllers();

// Add Swagger (OpenAPI generator)
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Enable controllers
app.MapControllers();

// Enable WebSockets globally
app.UseWebSockets();

// Resolve your processor
IMachineProcessor _iMachineProcessor = app.Services.GetRequiredService<IMachineProcessor>();
var socketHandler = new MyWebSocketHandler(_iMachineProcessor);

// Custom WebSocket middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/pub/chat")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
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

// Simple root endpoint
app.MapGet("/", () => "Connected!");

// Run app
await app.RunAsync();
