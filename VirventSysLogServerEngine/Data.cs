using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

namespace VirventSysLogServerEngine
{
    public class Data
    {
        public static SqlConnection GetConnection(string connectionString)
        {
            SqlConnection sqlConnection = new SqlConnection();
            sqlConnection.ConnectionString = connectionString;

            int waittime = 0;
            if (sqlConnection.State == ConnectionState.Broken)
            {
                sqlConnection.Close();
                while ((sqlConnection.State != ConnectionState.Closed) & (waittime < 20))
                {
                    System.Threading.Thread.Sleep(50);
                    waittime += 1;
                }
            }

            waittime = 0;
            if ((sqlConnection.State == ConnectionState.Closed))
            {
                sqlConnection.Open();
                while ((sqlConnection.State != ConnectionState.Open) & (waittime < 20))
                {
                    System.Threading.Thread.Sleep(50);
                    waittime += 1;
                }
            }

            waittime = 0;
            if (sqlConnection.State != ConnectionState.Open)
            {
                while ((sqlConnection.State != ConnectionState.Open) & (waittime < 20))
                {
                    System.Threading.Thread.Sleep(50);
                    waittime += 1;
                }
            }

            return sqlConnection;
        }

        public static void GenerateEntry(SqlConnection sqlConnection, SysLogMessage sysLogMessage)
        {
            if (sqlConnection.State != ConnectionState.Open)
                sqlConnection = Data.GetConnection(sqlConnection.ConnectionString);

            if (sqlConnection.State == ConnectionState.Open)
            {
                SqlCommand sqlCommand = new SqlCommand("", sqlConnection);
                Int64 thissdid = 0;
                if (sysLogMessage.SD != "-")
                {
                    foreach (DictionaryEntry de in sysLogMessage.sdParams)
                    {
                        string tsdid = ((string[])de.Key)[0];
                        string tsdpn = ((string[])de.Key)[1];
                        string tsdpv = (string)de.Value;
                        if (thissdid == 0)
                        {
                            sqlCommand.CommandText = "NewSD";
                            sqlCommand.CommandType = CommandType.StoredProcedure;
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue("@sdid", tsdid);
                            sqlCommand.Parameters.AddWithValue("@sdpn", tsdpn);
                            sqlCommand.Parameters.AddWithValue("@sdpv", tsdpv);
                            thissdid = (Int64)sqlCommand.ExecuteScalar();
                        }
                        else
                        {
                            sqlCommand.CommandText = "NextSD";
                            sqlCommand.CommandType = CommandType.StoredProcedure;
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue("@nsdid", thissdid);
                            sqlCommand.Parameters.AddWithValue("@sdid", tsdid);
                            sqlCommand.Parameters.AddWithValue("@sdpn", tsdpn);
                            sqlCommand.Parameters.AddWithValue("@sdpv", tsdpv);
                            sqlCommand.ExecuteScalar();
                        }
                    }
                }
                sqlCommand.CommandText = "NewLog";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Clear();
                sqlCommand.Parameters.AddWithValue("@timestamp", sysLogMessage.received.UtcDateTime);
                sqlCommand.Parameters.AddWithValue("@sourceip", sysLogMessage.senderIP);
                sqlCommand.Parameters.AddWithValue("@sourcename", sysLogMessage.sender.HostName);
                sqlCommand.Parameters.AddWithValue("@severity", sysLogMessage.severity);
                sqlCommand.Parameters.AddWithValue("@facility", sysLogMessage.facility);
                sqlCommand.Parameters.AddWithValue("@version", sysLogMessage.version);
                sqlCommand.Parameters.AddWithValue("@hostname", sysLogMessage.hostname);
                sqlCommand.Parameters.AddWithValue("@appname", sysLogMessage.appName);
                sqlCommand.Parameters.AddWithValue("@procid", sysLogMessage.procID);
                sqlCommand.Parameters.AddWithValue("@msgid", sysLogMessage.msgID);
                sqlCommand.Parameters.AddWithValue("@msgtimestamp", sysLogMessage.timestamp.UtcDateTime);
                sqlCommand.Parameters.AddWithValue("@msgoffset", sysLogMessage.timestamp.Offset.ToString());
                if (thissdid == 0)
                {
                    sqlCommand.Parameters.AddWithValue("@sdid", null);
                }
                else
                    sqlCommand.Parameters.AddWithValue("@sdid", thissdid);
                sqlCommand.Parameters.AddWithValue("@msg", sysLogMessage.msg);
                sqlCommand.ExecuteNonQuery();
            }
        }

    }
}
