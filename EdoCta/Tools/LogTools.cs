using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EdoCta.Tools
{
    public class LogTools
    {
        public static long RegisterLog(long parentId, string user, string empresa, string service, string serviceMethod, string info, DateTime date)
        {
            long logId = 0;
            int maxLen = 2048;
            if (info.Length > maxLen)
            {
                logId = InsertLog(parentId, user, empresa, service, serviceMethod, info.Substring(0, maxLen), DateTime.Now);
                int enviados = maxLen;

                while (enviados < info.Length)
                {
                    if ((info.Length - enviados) > maxLen)
                    {
                        InsertLog(parentId, user, empresa, service, serviceMethod, info.Substring(enviados, maxLen), DateTime.Now);
                    }
                    else
                    {
                        InsertLog(parentId, user, empresa, service, serviceMethod, info.Substring(enviados), DateTime.Now);
                    }
                    enviados += maxLen;
                }
            }
            else
            {
                logId = InsertLog(parentId, user, empresa, service, serviceMethod, info, DateTime.Now);
            }

            return logId;
        }

        private static long InsertLog(long parentId, string user, string empresa, string service, string serviceMethod, string info, DateTime date)
        {
            string insertCmd = @"INSERT INTO [HITEC].[dbo].[Log] ([parentId],[user],[app],[service],[serviceMethod],[info],[date])
                                VALUES (@parentId,@user,@app,@service,@serviceMethod,@info,@date); select @Identity=SCOPE_IDENTITY();";
            long id = 0;
            try
            {
                using (SqlConnection conexion = new SqlConnection(ConfigurationManager.ConnectionStrings["LOGDB"].ConnectionString))
                {
                    conexion.Open();
                    using (SqlCommand command = new SqlCommand(insertCmd, conexion))
                    {
                        command.Parameters.AddWithValue("@parentId", parentId);
                        command.Parameters.AddWithValue("@user", user);
                        command.Parameters.AddWithValue("@service", service);
                        command.Parameters.AddWithValue("@serviceMethod", serviceMethod);
                        command.Parameters.AddWithValue("@info", info);
                        command.Parameters.AddWithValue("@date", date);
                        command.Parameters.AddWithValue("@app", ConfigurationManager.AppSettings["App.Name"]);

                        SqlParameter parameter = command.Parameters.Add("@Identity", SqlDbType.BigInt);
                        parameter.Direction = ParameterDirection.Output;

                        command.ExecuteNonQuery();

                        id = (long)command.Parameters["@Identity"].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return id;
        }
    }
}
