namespace Attendance.WS.Models
{
    public class SendMessageInfo
    {
        public string ret { get; set; }
        public bool result { get; set; }
        public string cloudtime { get; set; }//DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        public string enrollid { get; set; }
        public int backupnum { get; set; }
        public int logindex { get; set; }
        public int count { get; set; }
        //"{\"ret\":\"sendlog\",\"result\":true,\"cloudtime\":\"" + server + "\"";
        //"{\"ret\":\"senduser\",\"result\":true,\"enrollid\":" + enrollid + ",\"backupnum\":" + backupnum + "}";
    }
}
