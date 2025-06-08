using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.DataAccess.Interfaces
{
    public interface IMachineDataProcessor
    {
        Task InsertMachineRawPunch(string DeviceSerialNo, string LogTime, string UserID);
    }
}
