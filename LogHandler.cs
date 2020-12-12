using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

namespace BRKMUD
{
    class LogHandler
    {
        protected static string logFile = "BRKLog.log";

        public static void CheckForLogFile() // Hello log file, are you there?
        {
            if (!File.Exists(logFile))
            {
                FileStream fs = File.Create(logFile);
                fs.Close();
                StreamWriter sw = new StreamWriter(logFile, true, Encoding.ASCII);
                sw.Write("Old log file not found...");
                sw.Write("\nCreating new log file...\n");
                sw.Write("Log created: "+DateTime.Now + "\n");
                sw.Close();
            }

        }

        public static void LogServerStart()
        {
            StreamWriter sw = new StreamWriter(logFile, true, Encoding.ASCII);
            sw.WriteLine("Server started " + NetworkServer.serverStarted);
            sw.Close();
        }

        public static void LogAClientEvent(string eventDescription, Client eventClient)
        {
            StreamWriter sw = new StreamWriter(logFile, true, Encoding.ASCII);
            sw.WriteLine(DateTime.Now + eventClient.clientUsername +" "+ eventDescription+" at " + eventClient.connectedOn.ToString());
            sw.Close();
        }

        public static void LogAServerEvent(string eventDescription)
        {
            StreamWriter sw = new StreamWriter(logFile, true, Encoding.ASCII);
            sw.WriteLine(DateTime.Now + "Server event: " + eventDescription);
            sw.Close();
        }

        public static void ChatLogger(Client talkingClient, DateTime passedTime, string userInput, string talkingChannel)
        {
            string chatTime = passedTime.ToString("dd/mm/yyyy H:mm:ss"); // This is for testing. A better alternative would be to just store the datetime as a datetime in the SQL database, instead of as Text as it is here.
            try
            {
                using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
                {
                    dbConnection.Open();

                    SqlCommand dbCommand = new SqlCommand("insert into chat (Timestamp, Username, Text, Channel) values (@Timestamp, @Username, @Text, @Channel)", dbConnection);

                    dbCommand.Parameters.Add("@Timestamp", SqlDbType.Text).Value = chatTime;
                    dbCommand.Parameters.Add("@Username", SqlDbType.Text).Value = talkingClient.clientUsername;
                    dbCommand.Parameters.Add("@Text", SqlDbType.Text).Value = userInput;
                    dbCommand.Parameters.Add("@Channel", SqlDbType.Text).Value = talkingChannel;
                    
                    dbCommand.ExecuteNonQuery();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
