using Attendance.WS.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net.WebSockets;
using System.Text;

namespace Attendance.WS
{
    public class MyWebSocketHandler : WebSocketHandler
    {
        private static int count = 0;
        protected override Task OnOpen(WebSocket socket)
        {
            count++;
            DataLogger.SaveTextToLog("Client connected!");
            return SendMessageAsync(socket, JsonConvert.SerializeObject("connected:" + count));
        }

        protected override Task OnMessage(WebSocket socket, string message)
        {
            DataLogger.SaveTextToLog("Message from client: " + message);
            JObject jsonMsg = JObject.Parse(message);
            var cmd = jsonMsg.Value<string>("cmd");
            var ret = jsonMsg.Value<string>("ret");
            
            SendMessageInfo objSendMessageInfo = new SendMessageInfo()
            {
                cloudtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                result = true,
                ret = cmd ?? ""
            };
            if (cmd == "reg")
            {
                var sns = jsonMsg.Value<string>("sn");
                if (ConnectedDevice.ConnectedDevices != null && ConnectedDevice.ConnectedDevices.Where(w => w.Key == sns).Count() > 0)
                {
                    ConnectedDevice.ConnectedDevices.Remove(sns);
                }
                ConnectedDevice.ConnectedDevices.Add(sns, this);
                //DataLogger.SaveTextToLog("Connected Device Is Here " + sns);
            }
            else if (cmd == "sendlog")
            {
                var sns = jsonMsg.Value<string>("sn");
                string enrollid = "0";
                String aliasid = "";
                var count = jsonMsg.Value<Int32>("count");
                var logindex = jsonMsg.Value<Int32>("logindex"); //add 2019-3-27 for sulotion the missing logs bug
                var attRecords = jsonMsg["record"];
                foreach (var ss in attRecords)
                {

                    enrollid = ss.Value<string>("enrollid");
                    if (ss.Value<String>("aliasid") != null)
                    {
                        enrollid = ss.Value<string>("aliasid");
                    }
                    var time = ss.Value<string>("time");
                    var mode = ss.Value<Int32>("mode");
                    var inout = ss.Value<Int32>("inout");
                    var ievent = ss.Value<Int32>("event");
                    string empname = ss.Value<string>("name");
                    DataLogger.SaveTextToLog("Alias = " + aliasid + " Enrollid = " + enrollid);
                    InsertLog(sns, time.ToString(), enrollid.ToString());
                    if (empname == null)
                    {
                        empname = "";
                    }
                }
            }
            else
            {
                DataLogger.SaveTextToLog("Cmd not identified: " + cmd);
            }
            return SendMessageAsync(socket, JsonConvert.SerializeObject(objSendMessageInfo));
        }

        protected override Task OnClose(WebSocket socket)
        {
            count--;
            DataLogger.SaveTextToLog("Client disconnected.");
            return Task.CompletedTask;
        }

        protected override Task OnError(WebSocket socket, Exception exception)
        {
            DataLogger.SaveTextToLog("WebSocket error: " + exception.Message);
            return Task.CompletedTask;
        }

        private int InsertLog(string DeviceSerialNo, string LogTime, string UserID)
        {
            const string constr = "Data Source=ACCULEKHAA_VM1;Initial Catalog=SmartAttendanceDB;User ID=WebAppUser;Password=WebAppUser@789; Integrated Security=False; Trusted_Connection=false; TrustServerCertificate=true";
            //const string constr = "Data Source=103.11.152.84;Initial Catalog=SmartAttendanceDB;Integrated Security=False;User ID=WebAppUser;Connect Timeout=15;Encrypt=False;Packet Size=4096; password=WebAppUser@789; Trusted_Connection=false; TrustServerCertificate=true";
            try
            {
                const int timeOut = 30;
                SqlCommand cmd = new SqlCommand("sp_InsertMachineRawPunch");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Cardno", UserID);
                cmd.Parameters.AddWithValue("@PunchTime", LogTime);
                cmd.Parameters.AddWithValue("@DeviceSerialNo", DeviceSerialNo);
                cmd.Parameters.AddWithValue("@Latitude", 0);
                cmd.Parameters.AddWithValue("@Longitude", 0);
                cmd.Parameters.AddWithValue("@IsManual", 0);

                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    cmd.Connection = con;
                    cmd.CommandTimeout = timeOut;
                    int result = cmd.ExecuteNonQuery();
                    con.Close();
                    return result;
                }
            }
            catch (Exception ex)
            {
                DataLogger.SaveTextToLog(ex.Message);

                if (ex.Message.Contains("Violation of PRIMARY KEY") || ex.Message.Contains("constraint"))
                {
                    return 0;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
