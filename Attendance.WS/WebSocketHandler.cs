using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace Attendance.WS
{
    public abstract class WebSocketHandler
    {
        public async Task HandleAsync(HttpContext context, WebSocket socket)
        {
            await OnOpen(socket);

            var buffer = new byte[1024 * 1024];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await OnClose(socket);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await OnMessage(socket, message);
                    }
                }
            }
            catch (Exception ex)
            {
                await OnError(socket, ex);
            }
        }

        protected virtual Task OnOpen(WebSocket socket)
        {
            DataLogger.SaveTextToLog("OnOpen: WebSocket connected");
            return Task.CompletedTask;
        }

        protected virtual Task OnMessage(WebSocket socket, string message)
        {
            DataLogger.SaveTextToLog($"OnMessage: {message}");
            return SendMessageAsync(socket, $"Echo: {message}");
        }

        protected virtual Task OnClose(WebSocket socket)
        {
            DataLogger.SaveTextToLog("OnClose: WebSocket closed");
            return Task.CompletedTask;
        }

        protected virtual Task OnError(WebSocket socket, Exception exception)
        {
            DataLogger.SaveTextToLog($"OnError: {exception.Message}");
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(WebSocket socket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            return socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
