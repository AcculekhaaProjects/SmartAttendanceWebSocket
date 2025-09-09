namespace Attendance.WS
{
    public static class ConnectedDevice
    {
        // Store both handler and WebSocket for each device
        public static Dictionary<string, (WebSocketHandler Handler, System.Net.WebSockets.WebSocket Socket)> ConnectedDevices = new Dictionary<string, (WebSocketHandler, System.Net.WebSockets.WebSocket)>();
        public static Dictionary<string, string> ConnectedDevicescmd = new Dictionary<string, string>();
    }
}
