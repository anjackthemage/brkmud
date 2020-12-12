using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Collections;
using System.Text;
using System.Data;
using System.Data.SqlClient;


namespace BRKMUD
{
    class NetworkServer
    {
        public static DateTime serverStarted;
        protected static string configFile = "BRKConfig.cfg";

        public static string connectionString = "Data Source=tcp:192.168.1.242\\SQLEXPRESS,1435; Initial Catalog=TestDB; User Id=testuser; Password=testpwd;"; // Perhaps this should be in a text file?

        public static ArrayList clients = new ArrayList(); // Clients who are connected and logged in.
        public static ArrayList clientsToLogIn = new ArrayList(); // Clients who are connected, but have not completed the log in process.
        public static ArrayList clientsToDisconnect = new ArrayList(); // This is used by the quit! command.
        public static ArrayList roomArray = new ArrayList(); // Have to store the rooms somewhere.

        static TcpListener receptionist;

        static void Main(string[] args) // Not sure if I want to take arguments yet, but keeping it here to remind myself
        {
            serverStarted = DateTime.Now;
            LogHandler.CheckForLogFile();
            LogHandler.LogServerStart();
            CheckForConfigFile();  // This is for future use.

            RoomHandler.loadAllRooms();

            IPAddress myip = Dns.GetHostAddresses("localhost")[0]; // I want it to give me the ipv4 addy, or maybe I'm having a problem using telnet to connect to an ipv6 addy?
            IPAddress mystaticipv4 = IPAddress.Parse("192.168.1.128"); // V V V This is such an effing pain. V V V
            receptionist = new TcpListener(mystaticipv4, 7022); // TcpListener(int port) will allow clients to connect to the server's ipv4 addy, but that method is considered 'deprecated' by visual studio :(
            // TCPListener(int port) will accept connections to any ip addy on the server
            receptionist.Start();

            Console.WriteLine("The receptionist is in. Lines are open at {0}, on port 7022.", myip); // Is it IPv6 notation because I'm using Windows 7?

            while (true)  // The main game loop
            {
                if (receptionist.Pending())  // The plan is to move this to a parallel thread
                {
                    TcpClient client = receptionist.AcceptTcpClient();
                                        
                    Thread t = new Thread(() => Client.CreateClient(client));  // Testing the idea of delegating the client creation to a seperate thread: So far, so good.
                    t.Start();
                    Console.WriteLine("We have a guest. They're in from " + client.Client.RemoteEndPoint); // This is fine for now, but this info should be appended to the client object for tracking purposes, also this should show the unique identifier for the client.
                }

                try
                {
                    foreach (Client dyingClient in clientsToDisconnect)
                    {
                        CloseClientConnection(dyingClient);
                    }
                    clientsToDisconnect.Clear();

                    foreach (Client loggingInClient in clientsToLogIn)
                    {
                        if (loggingInClient.loggedInFlag == true)
                            clients.Add(loggingInClient);
                    }

                    foreach (Client clientToReadWrite in clients)
                    {
                        //Console.WriteLine("In main foreach loop with" + clientToReadWrite.clientUsername);
                        if (clientsToLogIn.Contains(clientToReadWrite))
                            clientsToLogIn.Remove(clientToReadWrite);

                        if (clientToReadWrite.connectedClient.GetStream().DataAvailable)  // Is the connected client trying to send us input?
                        {
                            InputHandler.InputInterpreter(clientToReadWrite);
                        }
                        if (clientToReadWrite.disconnectFlag == true)
                        {
                            clientsToDisconnect.Add(clientToReadWrite);
                        }
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    LogHandler.LogAServerEvent(e.ToString());
                }

            }
        }

        static void CheckForConfigFile()  // For future implementation.
        {
            if (!File.Exists(configFile))
            {
                FileStream fs = File.Create(configFile);
                fs.Close();
                StreamWriter sw = new StreamWriter(configFile, true, Encoding.ASCII);
                sw.Write("Old config file not found...");
                sw.Write("\nCreating new config file...\n");
                sw.Write(DateTime.Now + "\n");
                sw.Close();
            }
        }

        static void CloseClientConnection(Client closingClient)
        {
            Console.WriteLine("Our guest has returned to {0}.", closingClient.clientEndPoint); // IPv6 :(
            closingClient.OnDisconnect();
            LogHandler.LogAClientEvent("disconnected", closingClient);
            
        }

    }
}
