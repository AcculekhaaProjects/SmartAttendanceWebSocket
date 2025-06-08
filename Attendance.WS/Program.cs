using Attendance.BusinessLogic.Interfaces;
using Attendance.WS;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebSocketDI();
var app = builder.Build();
app.UseWebSockets();

IMachineProcessor _iMachineProcessor = app.Services.GetRequiredService<IMachineProcessor>();
var socketHandler = new MyWebSocketHandler(_iMachineProcessor);

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

app.MapGet("/", () => "Connected!");

// Await the run method to keep the host alive
await app.RunAsync();