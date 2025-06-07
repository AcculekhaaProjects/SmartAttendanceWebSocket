namespace Attendance.WS.Models
{
    public class SendMessageInfo
    {
        public string ret { get; set; }
        public bool result { get; set; }
        public string cloudtime { get; set; }//DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        //"{\"ret\":\"sendlog\",\"result\":true,\"cloudtime\":\"" + server + "\"";
    }
}
