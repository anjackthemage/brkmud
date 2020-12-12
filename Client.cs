using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

namespace BRKMUD
{
    public class Client  // This happens in a seperate thread, otherwise the GateKeeper would halt the program while waiting for the user to
    // input their username and password. It might be possible to remove the need for this by using the input handler for all
    // input. The program could use states to determine if a connected client is 'logged in' or not. This would let the
    // program know what output to send to a user.
    {
        public IPAddress clientIP;
        public EndPoint clientEndPoint;
        public TcpClient connectedClient;

        public StreamReader incoming;
        public StreamWriter outgoing;
        public Stream daStream;

        public string clientUsername;
        public DateTime connectedOn;

        public bool disconnectFlag;
        public bool loggedInFlag;

        public bool isAdmin;

        public string accountStatus;

        public room currentRoom;
        private room lastRoom;

        public Client(TcpClient connectedClient)
        {

            //***************************************************
            // This is here to account for the extra bytes that fat ftp clients (putty, etc) will send upon an initial connection.
            // Eventually a proper telnet handler will exist and this won't be necessary.
            //byte[] bytesFromClient = new byte[32]; // Set the buffer size.


            //var testReader = connectedClient.GetStream().BeginRead(bytesFromClient, 0, bytesFromClient.Length, null, null); // Read all incoming bytes.
            //WaitHandle waiter = testReader.AsyncWaitHandle;
            //bool finishedRead = waiter.WaitOne(250, false); // If there are no bytes to read, we'll wait 1/4 of a second before continuing.
            //if (finishedRead)
            //{
            //    connectedClient.GetStream().EndRead(testReader);
            //    foreach (int i in bytesFromClient)
            //        Console.WriteLine("Read a byte:" + i); // This is for debugging.
            //}
            //else
            //{
                
            //    connectedClient.GetStream().EndRead(testReader);
            //}

            
            //***************************************************

            // Need to add a unique ID, possibly based on yearmonthdayhourminutesecondincrement pattern. This will be a session-unique ID,
            // but we also need an account-specific identifier.
            this.isAdmin = false;
            this.connectedOn = DateTime.Now; // When the client connected.
            this.disconnectFlag = false;
            this.loggedInFlag = false;
            this.clientUsername = "Unknown User";

            this.connectedClient = connectedClient;
            this.clientIP = IPAddress.Parse(((IPEndPoint)connectedClient.Client.RemoteEndPoint).Address.ToString());
            this.clientEndPoint = connectedClient.Client.RemoteEndPoint;
            NetworkServer.clientsToLogIn.Add(this);
            Console.WriteLine("Client connected from: " + clientIP);
            this.daStream = connectedClient.GetStream();
            this.incoming = new StreamReader(daStream);
            this.outgoing = new StreamWriter(daStream);
            this.outgoing.AutoFlush = true;  // Why?

            //this.daStream.ReadTimeout = 250;
            try
            {
                this.outgoing.Write("Press any key...");
                this.incoming.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //this.daStream.ReadTimeout = -1;
            

            InputHandler.DisplayTextFile(this, "WelcomeScreen.txt");  // Display the welcome screen when the client connects.
            // Also need a mechanic for creating accounts
            while (!GateKeeper(this))
            {
            }
            this.loggedInFlag = true;

            Console.WriteLine(this.clientUsername + " logged in.");
            LogHandler.LogAClientEvent("connected", this);  // Is it better to put this here or within the GateKeeper?

            this.outgoing.WriteLine("Welcome " + clientUsername);
            ChatServer.globalChannel.Add(this);


            using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))  // Move this to a method?
            {
                dbConnection.Open();
                SqlDataReader dbReader = null;
                SqlCommand dbCommand = new SqlCommand("select lastRoom from users where username like '" + clientUsername + "'", dbConnection);
                dbReader = dbCommand.ExecuteReader();
                dbReader.Read();
                string roomToFindInArrayList = dbReader["lastRoom"].ToString();
                lastRoom = RoomHandler.findRoom(roomToFindInArrayList);
                if (lastRoom == null)
                    lastRoom = RoomHandler.findRoom("TestRoom1");
            }
            onRoomChange(this, lastRoom);  // Make the client appear in the room there were in when they logged off.

            this.outgoing.Write("\nInput Please > ");

        }

        public void OnDisconnect()
        {
            this.connectedClient.Close();
            NetworkServer.clients.Remove(this);
            this.currentRoom.occupants.Remove(this);
            if (ChatServer.globalChannel.Contains(this))
                ChatServer.globalChannel.Remove(this);
            
        }

        public static bool GateKeeper(Client passedClient) // Right now the GateKeeper handles its own input, but I'd like to have the InputHandler handle ALL input
        {                                                   // Maybe we could use multiple overflows for InputInterpreter to specify where the input is coming from (passwords, etc.)
            try
            {
                passedClient.outgoing.Write("\nUsername > ");
                string username = passedClient.incoming.ReadLine();
                Console.WriteLine(username);
                if (username == "" || username == null)
                {
                    passedClient.outgoing.WriteLine("\nUsername cannot be blank.");
                    return false;
                }
                passedClient.outgoing.Write("Password > ");
                string password = passedClient.incoming.ReadLine();
                Console.WriteLine(password);
                if (password == "" || password == null)
                {
                    passedClient.outgoing.WriteLine("\nPassword cannot be blank.");
                    return false;
                }

                using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
                {
                    dbConnection.Open();
                    //Console.WriteLine("dbConnection State: " + dbConnection.State);  // For debugging purposes. Delete when no longer needed.
                    SqlDataReader dbReader = null;
                    SqlCommand dbCommand = new SqlCommand("select * from users where username like '" + username + "'", dbConnection);
                    dbReader = dbCommand.ExecuteReader();
                    string comparisonUsername = null;
                    string comparisonPassword = null;
                    while (dbReader.Read())  // I guess you have to enumerate this in order to get the stored values.
                    {
                        comparisonUsername = dbReader["username"].ToString();
                        comparisonPassword = dbReader["pass"].ToString();  // Need to move passwords to an encrypted database. Why? Encryptions can be broken, proper security should be controlled access.
                        if (dbReader["admin"].ToString() == "true")
                            passedClient.isAdmin = true;
                        passedClient.accountStatus = dbReader["status"].ToString();
                    }
                    if (comparisonUsername != null)
                    {
                        if (username.ToLower() == comparisonUsername.ToLower())  // Is this necessary?
                        {
                            if (password == comparisonPassword)
                            {
                                passedClient.clientUsername = username;
                                if (passedClient.accountStatus != "active")
                                {
                                    passedClient.outgoing.WriteLine("\nThis account is not currently active. It may have been banned.");
                                    passedClient.outgoing.WriteLine("Please contact an administrator for more information."); // Probably need to list an email addy here.
                                    return false;
                                }
                                if (NetworkServer.clients.Contains(InputHandler.FindClientByUsername(username))) //Here, check to see if the username is already logged in.
                                {
                                    passedClient.outgoing.WriteLine("\nThat client is already logged in.");
                                    return false;
                                }
                                return true;
                            }
                        }
                    }
                    passedClient.outgoing.Write("\nIncorrect password or username, is this a new user?[y/n]: "); // Need to call the NewUser method here.
                    if (passedClient.incoming.ReadLine().ToLower() == "y")
                    {
                        passedClient.outgoing.WriteLine("\nNew account creation through this interface has not yet been implemented.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }


        public static void CreateClient(TcpClient client)
        {
            new Client(client);
        }

        public void onRoomChange(Client changingClient, room newRoom)
        {
            newRoom.occupants.Add(changingClient);
            if (changingClient.currentRoom != null)
            {
                changingClient.currentRoom.occupants.Remove(changingClient);
            }
            changingClient.currentRoom = newRoom;
            changingClient.outgoing.WriteLine(changingClient.currentRoom.description);
            foreach (EXIT exit in changingClient.currentRoom.exits)
            {
                changingClient.outgoing.WriteLine(exit.exitDescription);
            }
            foreach (Client occupant in changingClient.currentRoom.occupants)
            {
                changingClient.outgoing.WriteLine(occupant.clientUsername);
            }
        }
    }
}