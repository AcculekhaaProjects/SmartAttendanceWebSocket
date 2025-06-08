using Attendance.BusinessLogic.Interfaces;
using Attendance.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.BusinessLogic.Processors
{
    public class MachineProcessor : IMachineProcessor
    {
        private readonly IMachineDataProcessor _iMachineDataProcessor;
        public MachineProcessor(IMachineDataProcessor machineDataProcessor) 
        {
            _iMachineDataProcessor = machineDataProcessor;
        }
        public Task InsertMachineRawPunch(string DeviceSerialNo, string LogTime, string UserID)
        {
            return _iMachineDataProcessor.InsertMachineRawPunch(DeviceSerialNo, LogTime, UserID);
        }
    }
}
