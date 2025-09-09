using Attendance.BusinessLogic.Interfaces;
using Attendance.Library;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Logging;
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
        private int count = 0;
        private readonly IMachineProcessor _iMachineProcessor;

        public string g_now_sn;

        public bool disablereturn;
        public bool enablereturn;
        public bool getuserlistreturn;
        public bool getuserinfoflag;
        public bool setuserlistreturn;
        public bool setuserinfoflag;
        public bool setholidayflag;

        public struct struct_userlist
        {
            public int enrollid;
            public int backupnum;
        }

        public struct_userlist[] str_userlist = new struct_userlist[20000];
        public int userlistindex;

        public struct struct_userinfo
        {
            public int enrollid;
            public int backupnum;
            public int admin;
            public string name;
            public uint password;
            public string fpdata;
        }

        public struct_userinfo tmpuserinfo;
        public MyWebSocketHandler(IMachineProcessor machineProcessor)
        {
            _iMachineProcessor = machineProcessor;
        }
        protected override Task OnOpen(WebSocket socket)
        {
            IncrementConnectionCount();
            DataLogger.SaveTextToLog("Client connected!");
            // Register both handler and socket for the device (sns will be set on first message)
            // The actual mapping to device serial number (sns) will be updated in OnMessage/reg
            return SendMessageAsync(socket, JsonConvert.SerializeObject("connected:" + count));
        }

        protected override Task OnClose(WebSocket socket)
        {
            DecrementConnectionCount();
            DataLogger.SaveTextToLog("Client disconnected.");
            return Task.CompletedTask;
        }

        private void IncrementConnectionCount()
        {
            count++;
        }

        private void DecrementConnectionCount()
        {
            count--;
        }

        protected override Task OnMessage(WebSocket socket, string message)
        {
            DataLogger.SaveTextToLog("Message from client: " + message);
            JObject jsonMsg = JObject.Parse(message);
            var cmd = jsonMsg.Value<string>("cmd");
            var ret = jsonMsg.Value<string>("ret");
            var sns = jsonMsg.Value<string>("sn") ?? "";
            
            string strRespone;

            if (!string.IsNullOrEmpty(cmd))
            {
                string enrollid;
                switch (cmd)
                {
                    case "reg":
                        if (!string.IsNullOrEmpty(sns))
                        {
                            if (ConnectedDevice.ConnectedDevices != null && ConnectedDevice.ConnectedDevices.ContainsKey(sns))
                            {
                                ConnectedDevice.ConnectedDevices.Remove(sns);
                            }
                            ConnectedDevice.ConnectedDevices.Add(sns, (this, socket));
                        }
                        strRespone = "{\"ret\":\"reg\",\"result\":true,\"cloudtime\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"}";
                        DataLogger.SaveTextToLog("Sending Message reg: " + strRespone);
                        return SendMessageAsync(socket, strRespone);
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
                        strRespone = "{\"ret\":\"sendlog\",\"result\":true,\"cloudtime\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"";
                        strRespone = strRespone + ",\"logindex\":" + logindex;
                        strRespone = strRespone + ",\"count\":" + logcount;
                        strRespone = strRespone + "}";
                        DataLogger.SaveTextToLog("Sending Message sendlog: " + strRespone);
                        return SendMessageAsync(socket, strRespone);
                    case "senduser":
                        enrollid = jsonMsg.Value<string>("enrollid") ?? "";
                        var backupnum = jsonMsg.Value<Int32>("backupnum");
                        strRespone = "{\"ret\":\"senduser\",\"result\":true,\"enrollid\":" + enrollid + ",\"backupnum\":" + backupnum + "}";
                        DataLogger.SaveTextToLog("Sending Message senduser: " + strRespone);
                        return SendMessageAsync(socket, strRespone);
                    default:
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(ret))
            {
                switch (ret)
                {
                    case "getuserlist":
                        var result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var count = jsonMsg.Value<Int32>("count");
                            var indexfrom = jsonMsg.Value<Int32>("from");
                            var indexto = jsonMsg.Value<Int32>("to");
                            var attRecords = jsonMsg["record"];
                            foreach (var ss in attRecords)
                            {
                                var enrollid = ss.Value<Int32>("enrollid");
                                var admin = ss.Value<Int32>("admin");
                                var backupnum = ss.Value<Int32>("backupnum");
                                str_userlist[userlistindex].enrollid = enrollid;
                                str_userlist[userlistindex].backupnum = backupnum;
                                userlistindex++;
                            }
                            if (indexto < count)
                            {
                                string cmdstring;
                                cmdstring = "{\"cmd\":\"getuserlist\",\"stn\":false}";
                                SendMessageAsync(socket, cmdstring);
                            }
                            else
                            {
                                getuserlistreturn = true;
                            }

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                            DataLogger.SaveTextToLog("getuserlist : " + reasoncode);
                        }
                        break;
                    case "getuserinfo":
                        result = jsonMsg.Value<bool>("result");
                        if (result)
                        {
                            tmpuserinfo.enrollid = jsonMsg.Value<Int32>("enrollid");
                            tmpuserinfo.name = jsonMsg.Value<string>("name"); //add version 1.1
                            tmpuserinfo.backupnum = jsonMsg.Value<Int32>("backupnum");
                            tmpuserinfo.admin = jsonMsg.Value<Int32>("admin");
                            if (tmpuserinfo.backupnum >= 20 && tmpuserinfo.backupnum < 28) //is face
                            {
                                tmpuserinfo.fpdata = jsonMsg.Value<string>("record");
                            }
                            else if (tmpuserinfo.backupnum >= 0 && tmpuserinfo.backupnum < 10) //is fp
                            {
                                tmpuserinfo.fpdata = jsonMsg.Value<string>("record");
                            }
                            else if (tmpuserinfo.backupnum == 10) //card
                            {
                                tmpuserinfo.password = jsonMsg.Value<uint>("record");
                            }
                            else if (tmpuserinfo.backupnum == 11) //pwd
                            {
                                tmpuserinfo.password = jsonMsg.Value<uint>("record");
                            }
                            else if (tmpuserinfo.backupnum == 50) //ai face photo base 64
                            {
                                tmpuserinfo.fpdata = jsonMsg.Value<string>("record");
                            }
                            getuserinfoflag = true;
                            DataLogger.SaveTextToLog($"getuserinfo {getuserinfoflag}: " + JsonConvert.SerializeObject(tmpuserinfo));
                        }
                        else
                        {
                            DataLogger.SaveTextToLog("getuserinfo false: " + jsonMsg.Value<Int32>("reason"));
                        }
                        break;
                    case "getallusers":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var count = jsonMsg.Value<Int32>("count");
                            var index = jsonMsg.Value<Int32>("index");
                            tmpuserinfo.enrollid = jsonMsg.Value<Int32>("enrollid");
                            tmpuserinfo.name = jsonMsg.Value<string>("name"); //add version 1.1
                            tmpuserinfo.backupnum = jsonMsg.Value<Int32>("backupnum");
                            tmpuserinfo.admin = jsonMsg.Value<Int32>("admin");
                            if (tmpuserinfo.backupnum >= 0 && tmpuserinfo.backupnum < 10) //is fp
                            {
                                tmpuserinfo.fpdata = jsonMsg.Value<string>("record");
                            }
                            else if (tmpuserinfo.backupnum == 10) //card
                            {
                                tmpuserinfo.password = jsonMsg.Value<uint>("record");
                            }
                            else if (tmpuserinfo.backupnum == 11) //pwd
                            {
                                tmpuserinfo.password = jsonMsg.Value<uint>("record");
                            }
                            else if (tmpuserinfo.backupnum == 50) //is aiface base64
                            {
                                tmpuserinfo.fpdata = jsonMsg.Value<string>("record");
                                byte[] rawjpg = Convert.FromBase64String(tmpuserinfo.fpdata);
                                System.IO.File.WriteAllBytes(@"C:\\EnrollPhoto\" + "LF" + tmpuserinfo.enrollid.ToString().PadLeft(8, '0') + ".jpg", rawjpg);
                            }
                            if (index < (count - 1))
                            {
                                string cmdstring;
                                cmdstring = "{\"cmd\":\"getallusers\",\"stn\":false}";
                                Console.WriteLine(cmdstring);
                                Console.WriteLine("index:" + index + ";count:" + count + ";");
                                SendMessageAsync(socket, cmdstring);
                            }

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "setuserinfo":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            setuserinfoflag = true;
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "deleteuser":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "cleanuser":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "getusername":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var nameutf8 = jsonMsg.Value<string>("record");
                            //utf8 change to gb2310 or ascii (option) maybe you can directly save utf8 char
                            //because http format usually is utf8
                            //var namegb2312 = LogHelper.utf8_gb2312(nameutf8);
                            Console.WriteLine(nameutf8);
                            ////////////////////////////////////////////
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "setusername":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "getnewlog":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var count = jsonMsg.Value<Int32>("count");
                            var indexfrom = jsonMsg.Value<Int32>("from");
                            var indexto = jsonMsg.Value<Int32>("to");
                            var attRecords = jsonMsg["record"];
                            foreach (var ss in attRecords)
                            {
                                var enrollid = ss.Value<Int32>("enrollid");
                                var time = ss.Value<string>("time");
                                var mode = ss.Value<Int32>("mode");
                                var inout = ss.Value<Int32>("inout");
                                var ievent = ss.Value<Int32>("event");
                            }
                            if (indexto < count)
                            {
                                string cmdstring;
                                cmdstring = "{\"cmd\":\"getnewlog\",\"stn\":false}";
                                SendMessageAsync(socket, cmdstring);
                            }

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "getalllog":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var count = jsonMsg.Value<Int32>("count");
                            var indexfrom = jsonMsg.Value<Int32>("from");
                            var indexto = jsonMsg.Value<Int32>("to");
                            var attRecords = jsonMsg["record"];
                            foreach (var ss in attRecords)
                            {
                                var enrollid = ss.Value<Int32>("enrollid");
                                var time = ss.Value<string>("time");
                                var mode = ss.Value<Int32>("mode");
                                var inout = ss.Value<Int32>("inout");
                                var ievent = ss.Value<Int32>("event");
                            }
                            if (indexto < count)
                            {
                                string cmdstring;
                                cmdstring = "{\"cmd\":\"getalllog\",\"stn\":false}";
                                SendMessageAsync(socket, cmdstring);
                            }

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "cleanlog":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "initsys":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "cleanadmin":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "setdevinfo":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "getdevinfo":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var deviceid = jsonMsg.Value<Int32>("deviceid");
                            var language = jsonMsg.Value<Int32>("language");
                            var volume = jsonMsg.Value<Int32>("volume");
                            var screensaver = jsonMsg.Value<Int32>("screensaver");
                            var verifymode = jsonMsg.Value<Int32>("verifymode");
                            var sleep = jsonMsg.Value<Int32>("sleep");
                            var userfpnum = jsonMsg.Value<Int32>("userfpnum");
                            var loghint = jsonMsg.Value<Int32>("loghint");
                            var reverifytime = jsonMsg.Value<Int32>("reverifytime");
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "opendoor":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "setdevlock":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "getdevlock":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var opendelay = jsonMsg.Value<Int32>("opendelay");
                            var doorsensor = jsonMsg.Value<Int32>("doorsensor");
                            var alarmdelay = jsonMsg.Value<Int32>("alarmdelay");
                            var threat = jsonMsg.Value<Int32>("threat");
                            var InputAlarm = jsonMsg.Value<Int32>("InputAlarm");
                            var antpass = jsonMsg.Value<Int32>("antpass");
                            var interlock = jsonMsg.Value<Int32>("interlock");
                            var mutiopen = jsonMsg.Value<Int32>("mutiopen");
                            var tryalarm = jsonMsg.Value<Int32>("tryalarm");
                            var tamper = jsonMsg.Value<Int32>("tamper");
                            var wgformat = jsonMsg.Value<Int32>("wgformat");
                            var wgoutput = jsonMsg.Value<Int32>("wgoutput");
                            var cardoutput = jsonMsg.Value<Int32>("cardoutput");
                            ///////////////// 
                            var dayzones = jsonMsg["dayzone"];
                            foreach (var days in dayzones)
                            {
                                JArray day = days.Value<JArray>("day");
                                foreach (var sections in day)
                                {
                                    var section = sections.Value<string>("section");
                                    //save the message
                                }
                            }
                            /////////////////////////////
                            var weekzones = jsonMsg["weekzone"];
                            foreach (var weeks in weekzones)
                            {
                                JArray week = weeks.Value<JArray>("week");
                                foreach (var days in week)
                                {
                                    var day = days.Value<Int32>("day");
                                    ///////
                                    ///save the message
                                }
                            }

                            var lockgroup = jsonMsg["lockgroup"]; //group //锁组合
                            foreach (var groups in lockgroup)
                            {
                                var group = groups.Value<Int32>("group");
                                ///////
                                ///save the message
                            }
                            /////////////////////////////
                            var nopentimes = jsonMsg["nopentime"]; //normal opn time zone //
                            foreach (var days in nopentimes)
                            {
                                var nday = days.Value<Int32>("day");
                                ///////
                                ///save the message
                            }

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "getuserlock":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var enrollid = jsonMsg.Value<Int32>("enrollid");
                            var weekzone = jsonMsg.Value<Int32>("weekzone");
                            var group = jsonMsg.Value<Int32>("group");
                            var startime = jsonMsg.Value<string>("starttime");
                            var endtime = jsonMsg.Value<string>("endtime");
                            DateTime startime2 = DateTime.Parse(startime);
                            DateTime endtime2 = DateTime.Parse(endtime);
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "setuserlock":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "deleteuserlock":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "cleanuserlock":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {

                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "disabledevice":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            disablereturn = true;
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "enabledevice":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            enablereturn = true;
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "getholiday":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            var holidayindex = jsonMsg.Value<Int32>("index");
                            var holidayname = jsonMsg.Value<string>("name");
                            var startday = jsonMsg.Value<string>("startdate");
                            var endday = jsonMsg.Value<string>("enddate");
                            var idallcout = jsonMsg.Value<Int32>("idallcout");
                            if (idallcout > 0)
                            {
                                var indexfrom = jsonMsg.Value<Int32>("indexfrom");
                                var indexto = jsonMsg.Value<Int32>("indexto");
                                var accessids = jsonMsg["accessid"];
                                foreach (var ids in accessids)
                                {
                                    var enrollid = ids;

                                }
                                if (indexto < idallcout)
                                {
                                    string cmdstring;
                                    cmdstring = "{\"cmd\":\"getholiday\",\"index\":" + holidayindex.ToString() + ",\"stn\":false}";
                                    SendMessageAsync(socket, cmdstring);
                                }

                            }
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "setholiday":
                        result = jsonMsg.Value<bool>("result");
                        if (result == true)
                        {
                            setholidayflag = true;
                        }
                        else if (result == false)
                        {
                            var reasoncode = jsonMsg.Value<Int32>("reason");
                        }
                        break;
                    case "deleteholiday":
                        break;
                    case "cleanholiday":
                        break;
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
        
        public void getuserlist(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;

            string cmdstring = "{\"cmd\":\"getuserlist\",\"stn\":true}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getuserinfo(string sn, int enrollid, int backupnum)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;

            string cmdstring = "{\"cmd\":\"getuserinfo\",\"enrollid\":" + enrollid + ",\"backupnum\":" + backupnum + "}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getallusers(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring = "{\"cmd\":\"getallusers\",\"stn\":true}"; //get all users
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void setuserinfo(string sn, int enrollid, string username, int backupnum, int admin, double carddata, string fpdata)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            ////////////////////////name mustbe utf8  ,if your saved name format is utf8,you can directly send 
            string usernameutf8 = HelperFunctions.gb2312_utf8(username);
            // string usernameutf8 =username;
            //////////////////////////////////////option end
            if (backupnum >= 20 && backupnum < 28) //is face
                cmdstring = "{\"cmd\":\"setuserinfo\",\"enrollid\":" + enrollid + ",\"name\":\"" + usernameutf8 + "\",\"backupnum\":" + backupnum + ",\"admin\":" + admin + ",\"record\":\"" + fpdata + "\"}";
            else if (backupnum >= 0 && backupnum < 10) //is fp
                cmdstring = "{\"cmd\":\"setuserinfo\",\"enrollid\":" + enrollid + ",\"name\":\"" + usernameutf8 + "\",\"backupnum\":" + backupnum + ",\"admin\":" + admin + ",\"record\":\"" + fpdata + "\"}";
            else if (backupnum == 10) //password
                cmdstring = "{\"cmd\":\"setuserinfo\",\"enrollid\":" + enrollid + ",\"name\":\"" + usernameutf8 + "\",\"backupnum\":" + backupnum + ",\"admin\":" + admin + ",\"record\":" + carddata + "}";
            else if (backupnum == 11) //card
                cmdstring = "{\"cmd\":\"setuserinfo\",\"enrollid\":" + enrollid + ",\"name\":\"" + usernameutf8 + "\",\"backupnum\":" + backupnum + ",\"admin\":" + admin + ",\"record\":" + carddata + "}";
            else if (backupnum == 50) //is aiface photo
                cmdstring = "{\"cmd\":\"setuserinfo\",\"enrollid\":" + enrollid + ",\"name\":\"" + usernameutf8 + "\",\"backupnum\":" + backupnum + ",\"admin\":" + admin + ",\"record\":\"" + fpdata + "\"}";
            else
                cmdstring = "";
            Console.WriteLine("setuserinfo =" + cmdstring + "len=" + cmdstring.Length);
            handler.SendMessageAsync(webSocket, cmdstring);
        }

        public void deleteuser(string sn, int enrollid, int backupnum)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"deleteuser\",\"enrollid\":" + enrollid + ",\"backupnum\":" + backupnum + "}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void cleanuser(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"cleanuser\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getusername(string sn, int enrollid)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getusername\",\"enrollid\":" + enrollid + "}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void setusername(string sn) //max send cout <=50
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring = "{\"cmd\":\"setusername\",\"count\":1,\"record\":[{\"enrollid\":1,\"name\":\"" + "Shailender" + "\"}]}";
            //cmdstring = "{\"cmd\":\"setusername\",\"count\":3,\"record\":[{\"enrollid\":1,\"name\":\"" + HelperFunctions.gb2312_utf8("邹春庆") + "\"},{\"enrollid\":2,\"name\":\"" + HelperFunctions.gb2312_utf8("周结官") + "\"},{\"enrollid\":3,\"name\":\"" + HelperFunctions.gb2312_utf8("吉川野子") + "\"}]}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }

        public void enableuser(string sn, int enrollid, bool enflag) //disable or enable user
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            ////////////////////////name mustbe utf8  ,if your saved name format is utf8,you can directly send 
            // string usernameutf8 = LogHelper.gb2312_utf8(username);
            //////////////////////////////////////option end
            if (enflag) //enable user
                cmdstring = "{\"cmd\":\"enableuser\",\"enrollid\":" + enrollid + ",\"enflag\":1}";
            else //disable
                cmdstring = "{\"cmd\":\"enableuser\",\"enrollid\":" + enrollid + ",\"enflag\":0}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getnewlog(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getnewlog\",\"stn\":true}"; //get new log
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getalllog(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getalllog\",\"stn\":true,\"from\":\"2000-01-01\",\"to\":\"2045-01-01\"}"; //get all log

            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void cleanlog(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"cleanlog\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void initsys(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"initsys\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void cleanadmin(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"cleanadmin\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void setdevinfo(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            int deviceid = 1;
            int language = 1;
            int volume = 8;
            int screensaver = 1;
            int verifymode = 0;
            int sleep = 0;
            int userfpnum = 3;
            int loghint = 1000;
            int reverifytime = 5;
            int disablemenu = 0;  //if you want to disable entermenu ,set 1
            cmdstring = "{\"cmd\":\"setdevinfo\",\"deviceid\":" + deviceid + ",\"language\":" + language + ",\"volume\":" + volume + ",\"screensaver\":" + screensaver + ",\"verifymode\":" + verifymode + ",\"sleep\":" + sleep + ",\"userfpnum\":" + userfpnum + ",\"loghint\":" + loghint + ",\"reverifytime\":" + reverifytime + ",\"disablemenu\":" + disablemenu + "}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getdevinfo(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getdevinfo\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void opendoor(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            //cmdstring = "{\"cmd\":\"opendoor\"}"; //this for normal access
            cmdstring = "{\"cmd\":\"opendoor\",\"doornum\":1}";  //this for access controller
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void setdevlock(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            int opendelay = 5;
            int doorsensor = 0;
            int alarmdelay = 0;
            int threat = 0;
            int InputAlarm = 0;
            int antpass = 0;
            int interlock = 0;
            int mutiopen = 0;
            int tryalarm = 0;
            int tamper = 0;
            int wgformat = 0;
            int wgoutput = 0;
            int cardoutput = 0;
            cmdstring = "{\"cmd\":\"setdevlock\",\"opendelay\":" + opendelay + ",\"doorsensor\":" + doorsensor + ",\"alarmdelay\":" + alarmdelay + ",\"threat\":" + threat + ",\"InputAlarm\":" + InputAlarm + ",\"antpass\":" + antpass + ",\"interlock\":" + interlock + ",\"mutiopen\":" + mutiopen + ",\"tryalarm\":" + tryalarm + ",\"tamper\":" + tamper + ",\"wgformat\":" + wgformat + ",\"wgoutput\":" + wgoutput + ",\"cardoutput\":" + cardoutput;
            int i, j;

            //////dayzone:
            int[,,] dayzone = new int[8, 5, 4];
            dayzone.Initialize();
            //"08:00~18:00"
            for (i = 0; i < 8; i++)
            {
                dayzone[i, 0, 0] = i + 1;
                dayzone[i, 0, 1] = 0;
            }
            cmdstring = cmdstring + ",\"dayzone\":[";
            for (i = 0; i < 8; i++)
            {
                cmdstring = cmdstring + "{\"day\":[";
                for (j = 0; j < 5; j++)
                {
                    string section = string.Format("{0:D2}:{1:D2}~{2:D2}:{3:D2}", dayzone[i, j, 0], dayzone[i, j, 1], dayzone[i, j, 2], dayzone[i, j, 3]);

                    cmdstring = cmdstring + "{\"section\":\"" + section + "\"}";
                    if (j < 4)
                        cmdstring = cmdstring + ","; //the enddiong item have no ","
                }
                cmdstring = cmdstring + "]}";
                if (i < 7)
                    cmdstring = cmdstring + ",";  //the enddiong item have no ","
            }
            cmdstring = cmdstring + "]";
            ///weekzone
            int[,] weekzone = new int[8, 7];
            weekzone.Initialize();
            for (i = 0; i < 8; i++)
            {
                for (j = 0; j < 7; j++)
                    weekzone[i, j] = i * 10 + j;
            }
            cmdstring = cmdstring + ",\"weekzone\":[";
            for (i = 0; i < 8; i++)
            {
                cmdstring = cmdstring + "{\"week\":[";
                for (j = 0; j < 7; j++)
                {
                    cmdstring = cmdstring + "{\"day\":" + weekzone[i, j] + "}";
                    if (j < 6)
                        cmdstring = cmdstring + ","; //the enddiong item have no ","
                }
                cmdstring = cmdstring + "]}";
                if (i < 7)
                    cmdstring = cmdstring + ","; //the enddiong item have no ","
            }
            cmdstring = cmdstring + "]";
            ///lockgroup
            int[] lockgroup = new int[5];
            lockgroup.Initialize();
            for (i = 0; i < 5; i++)
                lockgroup[i] = i;

            cmdstring = cmdstring + ",\"lockgroup\":[";
            for (i = 0; i < 5; i++)
            {
                cmdstring = cmdstring + "{\"group\":" + lockgroup[i] + "}";
                if (i < 4)
                    cmdstring = cmdstring + ","; //the enddiong item have no ","
            }
            cmdstring = cmdstring + "]";

            ///normal open time 常开门时段
            int[] opentmezone = new int[7];
            opentmezone.Initialize();
            for (i = 0; i < 7; i++)
                opentmezone[i] = 0;

            cmdstring = cmdstring + ",\"nopentime\":[";
            for (i = 0; i < 7; i++)
            {
                cmdstring = cmdstring + "{\"day\":" + opentmezone[i] + "}";
                if (i < 6)
                    cmdstring = cmdstring + ","; //the enddiong item have no ","
            }
            cmdstring = cmdstring + "]";
            //all end
            cmdstring = cmdstring + "}";

            Console.WriteLine("send:" + cmdstring);
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getdevlock(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getdevlock\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getuserlock(string sn, int enrollid)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getuserlock\",\"enrollid\":" + enrollid + "}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void setuserlock(string sn) //max send cout <=50
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            int weekzone = 1;
            //access access controller have 4 doors
            int weekzone2 = 1;
            int weekzone3 = 1;
            int weekzone4 = 1;
            /////////////////////
            int group = 1;
            string starttime = "2020-01-01 00:00:00";
            string endtime = "2099-12-30 00:00:00";
            //this sample show id=1 set all ; id=2 jsut set weekzone; id=3 just set group;
            //this for normal access
            //cmdstring = "{\"cmd\":\"setuserlock\",\"count\":3,\"record\":[{\"enrollid\":1,\"weekzone\":" + weekzone + "},{\"enrollid\":2,\"weekzone\":" + weekzone + ",\"group\":" + group + ",\"starttime\":\"" + starttime + "\",\"endtime\":\"" + endtime + "\"},{\"enrollid\":3,\"group\":" + group + "}]}";
            //this for access access controller
            cmdstring = "{\"cmd\":\"setuserlock\",\"count\":2,\"record\":[{\"enrollid\":1,\"weekzone\":" + weekzone + ",\"weekzone2\":" + weekzone2 + ",\"weekzone3\":" + weekzone3 + ",\"weekzone4\":" + weekzone4 + ",\"group\":" + group + ",\"starttime\":\"" + starttime + "\",\"endtime\":\"" + endtime + "\"},{\"enrollid\":2,\"weekzone\":" + weekzone + ",\"weekzone2\":" + weekzone2 + ",\"weekzone3\":" + weekzone3 + ",\"weekzone4\":" + weekzone4 + ",\"group\":" + group + ",\"starttime\":\"" + starttime + "\",\"endtime\":\"" + endtime + "\"}]}";

            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void deleteuserlock(string sn, int enrollid)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"deleteuserlock\",\"enrollid\":" + enrollid + "}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void cleanuserlock(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"cleanuserlock\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }

        public void reboot(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"reboot\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void settime(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            DateTime server = DateTime.Now;
            cmdstring = "{\"cmd\":\"settime\",\"cloudtime\":\"" + server.ToString("yyyy-MM-dd HH:mm:ss") + "\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void disabledevice(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"disabledevice\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void enabledevice(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"enabledevice\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void getholiday(string sn, int holidayindex)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getholiday\",\"index\":" + holidayindex.ToString() + ",\"stn\":true}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void setholiday(string sn, int holidayindex, bool stn, int accessidcount, int[] accessidbuf)
        {

            /////////////
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            string strstn = "true";
            if (stn == false)
                strstn = "false";

            cmdstring = "{\"cmd\":\"setholiday\",\"index\":" + holidayindex.ToString() + ",\"stn\":" + strstn + "," +
                         "\"startdate\":\"2018-09-19\",\"enddate\":\"2018-09-19\",\"name\":\"nation day\"," +
                         "\"count\":" + accessidcount.ToString() + ",\"accessid\":[";
            for (int i = 0; i < accessidcount; i++) //max 200 for one package, because the all len of one package need less then 3000
            {
                if (i == (accessidcount - 1))
                    cmdstring = cmdstring + accessidbuf[i].ToString();
                else
                    cmdstring = cmdstring + accessidbuf[i].ToString() + ",";
            }
            cmdstring = cmdstring + "]}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void deleteholiday(string sn, int holidayindex)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"deleteholiday\",\"index\":" + holidayindex.ToString() + "}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void cleanholiday(string sn)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"cleanholiday\"}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }
        public void adduser(string sn, int enrollid, int backupnum, int admin, string username)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string usernameutf8 = HelperFunctions.gb2312_utf8(username);
            string cmdstring;
            cmdstring = "{\"cmd\":\"adduser\",\"enrollid\":" + enrollid + ",\"backupnum\":" + backupnum + ",\"admin\":" + admin + ",\"name\":\"" + usernameutf8 + "\",\"flag\":10}";
            handler.SendMessageAsync(webSocket, cmdstring);
        }

        public void setuserprofile(string sn, int enrollid, string userprofile)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string userprofileutf8 = HelperFunctions.gb2312_utf8(userprofile);
            string cmdstring;
            cmdstring = "{\"cmd\":\"setuserprofile\",\"enrollid\":" + enrollid + ",\"profile\":\"" + userprofileutf8 + "\"}"; //if enrollid=0 ,is notice ,for machine alway display
            handler.SendMessageAsync(webSocket, cmdstring);
        }

        public void getuserprofile(string sn, int enrollid)
        {
            var deviceTuple = ConnectedDevice.ConnectedDevices[sn];
            var webSocket = deviceTuple.Socket;
            var handler = deviceTuple.Handler;
            string cmdstring;
            cmdstring = "{\"cmd\":\"getuserprofile\",\"enrollid\":" + enrollid + "}"; //if enrollid=0 ,is notice
            handler.SendMessageAsync(webSocket, cmdstring);
        }

    }
}
