using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.BusinessLogic.Interfaces
{
    public interface IMachineProcessor
    {
        Task InsertMachineRawPunch(string DeviceSerialNo, string LogTime, string UserID);
    }
}
