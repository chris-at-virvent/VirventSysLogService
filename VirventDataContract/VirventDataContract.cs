using System;
using System.Data;
using System.Data.SqlClient;
using VirventPluginContract;

namespace VirventDataContract
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

        public static void GenerateEntry(SqlConnection sqlConnection, Message sysLogMessage)
        {
            if (sqlConnection.State != ConnectionState.Open)
                sqlConnection = Data.GetConnection(sqlConnection.ConnectionString);

            if (sqlConnection.State == ConnectionState.Open)
            {
                SqlCommand sqlCommand = new SqlCommand("", sqlConnection);
                sqlCommand.CommandText = "ssp_CreateLogEntry";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Clear();

                sqlCommand.Parameters.AddWithValue("@Received", sysLogMessage.Received);
                sqlCommand.Parameters.AddWithValue("@SenderIPAddress", sysLogMessage.Sender.HostName.Trim());
                sqlCommand.Parameters.AddWithValue("@Severity", sysLogMessage.Severity);
                sqlCommand.Parameters.AddWithValue("@Facility", sysLogMessage.Facility);
                sqlCommand.Parameters.AddWithValue("@Version", sysLogMessage.Version);
                sqlCommand.Parameters.AddWithValue("@Host", sysLogMessage.Host == null ? sysLogMessage.Sender.HostName : sysLogMessage.Host);
                sqlCommand.Parameters.AddWithValue("@Application", sysLogMessage.AppName.Trim());
                sqlCommand.Parameters.AddWithValue("@RuleMessage", sysLogMessage.RuleMessage.Trim());
                sqlCommand.Parameters.AddWithValue("@RuleData", sysLogMessage.RuleData.Trim());
                sqlCommand.Parameters.AddWithValue("@Classification", sysLogMessage.Classification.Trim());
                sqlCommand.Parameters.AddWithValue("@Priority", sysLogMessage.Priority);
                sqlCommand.Parameters.AddWithValue("@SourceIP", sysLogMessage.SourceIP);
                sqlCommand.Parameters.AddWithValue("@SourcePort", sysLogMessage.SourcePort);
                sqlCommand.Parameters.AddWithValue("@DestinationIP", sysLogMessage.DestIP);
                sqlCommand.Parameters.AddWithValue("@DestinationPort", sysLogMessage.DestPort);
                sqlCommand.ExecuteNonQuery();
            }
        }

        public static int GetEntries(SqlConnection sqlConnection, int Severity, string HostName, string IPAddress, DateTimeOffset oldestDateToLook)
        {
            // gets a count of records meeting the query parameters
            if (sqlConnection.State != ConnectionState.Open)
                sqlConnection = Data.GetConnection(sqlConnection.ConnectionString);

            if (sqlConnection.State == ConnectionState.Open)
            {
                SqlCommand sqlCommand = new SqlCommand("", sqlConnection);
                sqlCommand.CommandText = "ssp_GetIPCount";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Clear();

                sqlCommand.Parameters.AddWithValue("@Received", oldestDateToLook.DateTime);
                sqlCommand.Parameters.AddWithValue("@Severity", Severity);
                sqlCommand.Parameters.AddWithValue("@Host", HostName);
                sqlCommand.Parameters.AddWithValue("@SourceIP", IPAddress);
                int result = (int)sqlCommand.ExecuteScalar();
                return result;
            }

            return 0;
        }
    }
}