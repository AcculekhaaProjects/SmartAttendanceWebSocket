using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.Models.ApiModels
{
    public class ActionResponseInfo
    {
        public string Status { get; set; }
        public string Msg { get; set; }
        public List<Dictionary<string, object>> data { get; set; }
        public List<string> RowData { get; set; }
    }
}
