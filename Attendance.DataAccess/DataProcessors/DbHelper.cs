using Attendance.DataAccess.Interfaces;
using Attendance.Library.Constants;
using Attendance.Models.DbModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.DataAccess.DataProcessor
{
    public class DbHelper : IDbHelper
    {
        private readonly string constr;
        public DbHelper(IConfiguration configuration)
        {
            constr = configuration[AppSettingConstants.DbDefaultConnectionString];
        }
        public async Task<DataTable> GetDataTableByQuery(string query)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.Fill(dt);
                con.Close();
                return dt;
            }
        }
        public async Task<DataTable> GetDataTableBySQLCommandAsync(SqlCommand cmd)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    await con.OpenAsync();
                    cmd.Connection = con;
                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = cmd;
                    await Task.Run(() => da.Fill(dt));
                    return dt;
                }
            }
            catch
            {
                throw;
            }
        }
        public async Task<DataSet> GetDataSetBySQLCommandAsync(SqlCommand cmd)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    await con.OpenAsync();
                    cmd.Connection = con;
                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = cmd;
                    await Task.Run(() => da.Fill(ds));
                    return ds;
                }
            }
            catch
            {
                throw;
            }
        }
        public async Task<int> ExecuteNonQueryBySQLCommandAsync(SqlCommand cmd)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                await con.OpenAsync();
                cmd.Connection = con;
                cmd.CommandTimeout = 20;//seconds
                int result = await cmd.ExecuteNonQueryAsync();
                con.Close();
                return result;
            }
        }
        public async Task<string> ExecuteScalarBySQLCommand(SqlCommand cmd)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();
                cmd.Connection = con;
                var result = cmd.ExecuteScalar();
                con.Close();
                return Convert.ToString(result);
            }
        }
        public List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }
        private T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    //in case you have a enum/GUID datatype in your model
                    //We will check field's dataType, and convert the value in it.
                    if (pro.Name == column.ColumnName)
                    {
                        try
                        {
                            //var convertedValue = GetValueByDataType(pro.PropertyType, dr[column.ColumnName]);
                            pro.SetValue(obj, dr[column.ColumnName], null);
                        }
                        catch (Exception e)
                        {
                            //ex handle code                   
                            //throw;
                            var convertedValue = GetValueByDataType(pro.PropertyType, dr[column.ColumnName]);
                            pro.SetValue(obj, convertedValue, null);
                        }
                        //pro.SetValue(obj, dr[column.ColumnName], null);
                    }
                    else
                        continue;
                }
            }
            return obj;
        }
        private static object GetValueByDataType(Type propertyType, object o)
        {
            if (string.IsNullOrEmpty(o.ToString()))
            {
                if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
                {
                    return Guid.NewGuid();
                }
                else if (propertyType == typeof(int) || propertyType.IsEnum)
                {
                    return 0;
                }
                else if (propertyType == typeof(decimal))
                {
                    return 0;
                }
                else if (propertyType == typeof(long))
                {
                    return 0;
                }
                else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
                {
                    return 0;
                }
                else if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                {
                    return 0;
                }
                else if (propertyType == typeof(double))
                {
                    return 0;
                }
                else
                {
                    return Convert.ToString(o);
                }
            }
            else
            {
                if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
                {
                    return Guid.Parse(o.ToString());
                }
                else if (propertyType == typeof(int) || propertyType.IsEnum)
                {
                    return Convert.ToInt32(o);
                }
                else if (propertyType == typeof(decimal))
                {
                    return Convert.ToDecimal(o);
                }
                else if (propertyType == typeof(long))
                {
                    return Convert.ToInt64(o);
                }
                else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
                {
                    return Convert.ToBoolean(o);
                }
                else if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                {
                    return Convert.ToDateTime(o);
                }
                else if (propertyType == typeof(double))
                {
                    return Convert.ToInt32(o);
                }
            }
            return o.ToString();
        }
        public async Task<DbResponseInfo> GetDbResponse(string result)
        {
            DbResponseInfo objDBResponseInfo;
            if (!string.IsNullOrEmpty(result))
            {
                if (result.Split(':').Length == 2)
                {
                    objDBResponseInfo = new DbResponseInfo()
                    {
                        Status = result.Split(':')[0],
                        Message = result.Split(':')[1]
                    };
                }
                else
                {
                    objDBResponseInfo = new DbResponseInfo()
                    {
                        Status = StatusConstants.Failure,
                        Message = result
                    };
                }
            }
            else
            {
                objDBResponseInfo = new DbResponseInfo()
                {
                    Status = StatusConstants.Failure,
                    Message = ApiConstants.SomethingWentWrong
                };
            }

            return objDBResponseInfo;
        }
    }
}
