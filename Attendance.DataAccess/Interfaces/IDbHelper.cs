using Attendance.Models.DbModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.DataAccess.Interfaces
{
    public interface IDbHelper
    {
        Task<DataTable> GetDataTableByQuery(string query);
        Task<DataTable> GetDataTableBySQLCommandAsync(SqlCommand cmd);
        Task<DataSet> GetDataSetBySQLCommandAsync(SqlCommand cmd);
        Task<int> ExecuteNonQueryBySQLCommandAsync(SqlCommand cmd);
        Task<string> ExecuteScalarBySQLCommand(SqlCommand cmd);
        List<T> ConvertDataTable<T>(DataTable dt);
        Task<DbResponseInfo> GetDbResponse(string result);
    }
}
