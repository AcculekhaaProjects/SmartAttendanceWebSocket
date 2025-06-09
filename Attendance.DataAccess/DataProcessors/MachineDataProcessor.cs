using Attendance.DataAccess.Interfaces;
using Attendance.Library.Constants;
using Attendance.Models.DbModels;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.DataAccess.DataProcessors
{
    public class MachineDataProcessor : IMachineDataProcessor
    {
        private readonly IDbHelper _iDbHelper;
        public MachineDataProcessor(IDbHelper dbHelper)
        {
            _iDbHelper = dbHelper;
        }

        public async Task InsertMachineRawPunch(string DeviceSerialNo, string LogTime, string UserID)
        {
            try
            {
                const int timeOut = 30;
                SqlCommand cmd = new SqlCommand(DBConstants.sp_InsertMachineRawPunch);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = timeOut;
                cmd.Parameters.AddWithValue("@Cardno", UserID);
                cmd.Parameters.AddWithValue("@PunchTime", LogTime);
                cmd.Parameters.AddWithValue("@DeviceSerialNo", DeviceSerialNo);
                cmd.Parameters.AddWithValue("@Latitude", 0);
                cmd.Parameters.AddWithValue("@Longitude", 0);
                cmd.Parameters.AddWithValue("@IsManual", 0);
                var result = await _iDbHelper.ExecuteScalarBySQLCommand(cmd);
            }
            catch
            {
                throw;
            }
        }
    }
}
