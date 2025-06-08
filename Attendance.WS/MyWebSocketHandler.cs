using Attendance.BusinessLogic.Interfaces;
using Attendance.WS.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace Attendance.WS
{
    public class MyWebSocketHandler : WebSocketHandler
    {
        private static int count = 0;
        private readonly IMachineProcessor _iMachineProcessor;
        public MyWebSocketHandler(IMachineProcessor machineProcessor)
        {
            _iMachineProcessor = machineProcessor;
        }
        protected override Task OnOpen(WebSocket socket)
        {
            IncrementConnectionCount();
            DataLogger.SaveTextToLog("Client connected!");
            return SendMessageAsync(socket, JsonConvert.SerializeObject("connected:" + count));
        }

        protected override Task OnClose(WebSocket socket)
        {
            DecrementConnectionCount();
            DataLogger.SaveTextToLog("Client disconnected.");
            return Task.CompletedTask;
        }

        private static void IncrementConnectionCount()
        {
            count++;
        }

        private static void DecrementConnectionCount()
        {
            count--;
        }

        protected override Task OnMessage(WebSocket socket, string message)
        {
            DataLogger.SaveTextToLog("Message from client: " + message);
            SendMessageInfo objSendMessageInfo = new SendMessageInfo();
            JObject jsonMsg = JObject.Parse(message);
            var cmd = jsonMsg.Value<string>("cmd");
            var ret = jsonMsg.Value<string>("ret");
            var sns = jsonMsg.Value<string>("sn") ?? "";
            string enrollid;

            if (!string.IsNullOrEmpty(cmd))
            {
                switch (cmd)
                {
                    case "reg":
                        if (ConnectedDevice.ConnectedDevices != null && ConnectedDevice.ConnectedDevices.Where(w => w.Key == sns).Count() > 0)
                        {
                            ConnectedDevice.ConnectedDevices.Remove(sns);
                        }
                        ConnectedDevice.ConnectedDevices.Add(sns, this);
                        objSendMessageInfo.cloudtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        objSendMessageInfo.result = true;
                        objSendMessageInfo.ret = "reg";
                        DataLogger.SaveTextToLog("Sending Message reg: " + JsonConvert.SerializeObject(objSendMessageInfo));
                        return SendMessageAsync(socket, JsonConvert.SerializeObject(objSendMessageInfo));
                    case "sendlog":
                        String aliasid = "";
                        var logcount = jsonMsg.Value<Int32>("count");
                        var logindex = jsonMsg.Value<Int32>("logindex");
                        var attRecords = jsonMsg["record"];
                        foreach (var ss in attRecords)
                        {
                            enrollid = ss.Value<string>("enrollid") ?? "";
                            if (ss.Value<String>("aliasid") != null)
                            {
                                enrollid = ss.Value<string>("aliasid") ?? "";
                            }
                            var time = ss.Value<string>("time");
                            DataLogger.SaveTextToLog("Alias = " + aliasid + " Enrollid = " + enrollid);
                            _iMachineProcessor.InsertMachineRawPunch(sns, time.ToString(), enrollid.ToString());
                        }
                        objSendMessageInfo.cloudtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        objSendMessageInfo.result = true;
                        objSendMessageInfo.ret = "sendlog";
                        objSendMessageInfo.logindex = logindex;
                        objSendMessageInfo.count = logcount;
                        DataLogger.SaveTextToLog("Sending Message sendlog: " + JsonConvert.SerializeObject(objSendMessageInfo));
                        return SendMessageAsync(socket, JsonConvert.SerializeObject(objSendMessageInfo));
                    case "senduser":
                        enrollid = jsonMsg.Value<string>("enrollid") ?? "";
                        var backupnum = jsonMsg.Value<Int32>("backupnum");
                        objSendMessageInfo.cloudtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        objSendMessageInfo.result = true;
                        objSendMessageInfo.ret = "senduser";
                        objSendMessageInfo.enrollid = enrollid;
                        objSendMessageInfo.backupnum = backupnum;
                        DataLogger.SaveTextToLog("Sending Message senduser: " + JsonConvert.SerializeObject(objSendMessageInfo));
                        return SendMessageAsync(socket, JsonConvert.SerializeObject(objSendMessageInfo));
                    default:
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(ret))
            {
                switch (ret)
                {
                    case "getallusers":
                        return SendMessageAsync(socket, "");
                    default:
                        break;
                }
            }

            // Ensure all code paths return a value
            return Task.CompletedTask;
        }

        protected override Task OnError(WebSocket socket, Exception exception)
        {
            DataLogger.SaveTextToLog("WebSocket error: " + exception.Message);
            return Task.CompletedTask;
        }

        
    }
}
