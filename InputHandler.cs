using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace BRKMUD
{
    public static class InputHandler
    {
        public static void InputInterpreter(Client commandingClient)
        {
            string clientInput = "";    // Pliant clients give silent compliance...
            string parsedCommand = "";
            string adjustedClientInput = "";

            List<string> commandList = new List<string> { "quit!", "dog", "say", "help", "chist", "lusers" };
            List<string> adminCommandList = new List<string> { "nuser", "laccounts", "kick", "ban", "unban", "xmltest", "loadcheck", "findroom" };
            List<string> verbList = new List<string> { "quit!", "say", "kick", "ban", "unban" };

            clientInput = commandingClient.incoming.ReadLine();  // Get the input.
            ArrayList commandingClients = new ArrayList();

            try
            {
                if (Regex.Match(clientInput, "^[/:]{1}").Success && commandingClient.isAdmin)
                {
                    commandingClient.outgoing.WriteLine("You typed an admin command indicator (/,:).");
                    commandingClient.outgoing.WriteLine("These commands are not yet implemented.");
                }//if (clientInput.IndexOf(" ") != -1) // Three ways I can see to do this: These two and a regex. What would be the least resource-intensive?
                else if (clientInput.Contains(' ')) // If there's a space, then there are multiple words in the string and the first word might be a command.
                {
                    parsedCommand = Regex.Match(clientInput, "^\\S*\\b{1}").Value; // Parse the first word out of the string, below we'll compare it against our list of commands to see if it's a valid command.
                    adjustedClientInput = Regex.Replace(clientInput, "^\\S*\\s{1}", string.Empty);  // And here's the content of the supposed command.
                }
                else
                {
                    parsedCommand = clientInput.ToLower(); // If there are no spaces, then the input is all one word, and we'll check to see if THAT is a command.
                    // first check to see if the player is trying to exit the room
                    foreach (EXIT exitEnum in commandingClient.currentRoom.exits)
                    {
                        if (exitEnum.exitName.ToLower() == parsedCommand.ToLower())
                        {
                            RoomHandler.changeRoom(commandingClient, RoomHandler.findRoom(exitEnum.exitDestination));
                            parsedCommand = "roomchange";
                            break;
                        }
                    }
                }

                //Input method to check the parsed command against any special commands defined by the room the player is currently in.

                if (adminCommandList.Contains(parsedCommand))
                {
                    if (commandingClient.isAdmin)
                    {
                        switch (parsedCommand) // What would be a better method of listing commands? // For admin commands, this needs to check for privilege level.
                        {                      // Maybe listing the commands in a database table and polling for something that matches the input?
                            case "nuser":
                                NewUser(commandingClient);
                                break;
                            case "laccounts":  // List all usernames in the database. In the future I'll add a few flags to these db entries which will be displayed here.
                                ListAccounts(commandingClient);
                                break;
                            case "kick":
                                KickUser(commandingClient, adjustedClientInput);
                                break;
                            case "ban":
                                BanUser(commandingClient, adjustedClientInput);
                                break;
                            case "unban":
                                UnBanUser(commandingClient, adjustedClientInput);
                                break;
                            case "xmltest":
                                RoomHandler.readRoom(commandingClient, "../../TestRoom1.xml");
                                break;
                            case "loadcheck":
                                commandingClient.outgoing.WriteLine("Reading room data...");
                                RoomHandler.showLoadedRooms(commandingClient);
                                commandingClient.outgoing.WriteLine("Finished.");
                                break;
                            case "findroom":
                                commandingClient.outgoing.WriteLine("Looking for room: " + adjustedClientInput);
                                commandingClient.outgoing.WriteLine(RoomHandler.findRoom(adjustedClientInput).roomName);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        commandingClient.outgoing.WriteLine("\nYou must be an admin to use that command.");
                    }
                }
                else // if(commandList.Contains(parsedCommand))  // Can add this in if we want to give the user an error when they give input that is not in the command list, instead of simply treating it like a Say command.
                {
                    switch (parsedCommand.ToLower())
                    {
                        case "quit!":
                            commandingClient.disconnectFlag = true;
                            break;
                        case "dog":
                            commandingClient.outgoing.WriteLine("\nYou found the secret word!");  // Because, dammit!
                            break;
                        case "say":
                            ChatServer.Chat(commandingClient, adjustedClientInput, "Say");
                            break;
                        case "tell":
                            string tellTarget = Regex.Match(adjustedClientInput, "^\\S*\\b{1}").Value;
                            adjustedClientInput = Regex.Replace(adjustedClientInput, "^\\S*\\s{1}", string.Empty);
                            Console.WriteLine(tellTarget + ":" + adjustedClientInput);
                            ChatServer.Tell(commandingClient, tellTarget, adjustedClientInput);
                            break;
                        case "help":
                            commandingClients.Add(commandingClient);
                            OutputHandler.fileOutputHandler("HelpFile.txt", commandingClients);
                            commandingClients.Remove(commandingClient);
                            break;
                        case "chist":
                            DisplayChatHistory(commandingClient);
                            break;
                        case "lusers":
                            ListUsers(commandingClient);
                            break;
                        case "roomchange":
                            break;
                        default:
                            ChatServer.Chat(commandingClient, clientInput, "Say");  // If it's not in the recognized command list, treat it like the clientInput was preceeded by the Say command.
                            break;
                    }
                }
                if (!NetworkServer.clientsToDisconnect.Contains(commandingClient))
                    commandingClient.outgoing.Write("\nInput Please > ");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LogHandler.LogAClientEvent(e.ToString(), commandingClient);
            }
        }

        static void ListAccounts(Client commandingClient)  // Lists all the accounts currently in the database, whether they have admin rights and their current status.
        {
            using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
            {
                dbConnection.Open();
                SqlDataReader dbReader = null;
                SqlCommand dbCommand = new SqlCommand("select username, admin, status from users", dbConnection);
                dbReader = dbCommand.ExecuteReader();
                commandingClient.outgoing.WriteLine("\nUser\tAdmin\tStatus");
                while (dbReader.Read())
                {
                    commandingClient.outgoing.WriteLine(dbReader["username"].ToString() + "\t" + dbReader["admin"].ToString() + "\t" + dbReader["status"].ToString());
                }
            }
        }

        static void ListUsers(Client commandingClient)
        {
            foreach (Client clientToList in NetworkServer.clients)
                commandingClient.outgoing.WriteLine("\n" + clientToList.clientUsername);  // List all userers currently connected.
        }

        static void DisplayChatHistory(Client commandingClient)  // Need to fix this to only display the last XX lines of chat history.
        {
            try
            {
                using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
                {
                    dbConnection.Open();
                    SqlDataReader dbReader = null;
                    SqlCommand dbCommand = new SqlCommand("Select * from Chat", dbConnection);
                    dbReader = dbCommand.ExecuteReader();
                    while (dbReader.Read())
                    {
                        commandingClient.outgoing.Write(dbReader["Timestamp"].ToString() + " ");
                        commandingClient.outgoing.Write(dbReader["Username"].ToString() + ": ");
                        commandingClient.outgoing.Write("\"" + dbReader["Text"].ToString() + "\"");
                        //commandingClient.outgoing.Write(dbReader["Channel"].ToString());
                        commandingClient.outgoing.WriteLine("");
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LogHandler.LogAClientEvent(e.ToString(), commandingClient);
            }
        }

        public static void DisplayTextFile(Client commandingClient, string fileToDisplay)
        {
            if (File.Exists(fileToDisplay))
            {
                //commandingClient.outgoing.Write(File.ReadAllText(fileToDisplay));
                string[] textFileToDisplay = File.ReadAllLines(fileToDisplay);
                commandingClient.outgoing.WriteLine(textFileToDisplay[0]);  // otherwise the if check will fire on the 0th iteration
                for (int i = 1; i < textFileToDisplay.Length; i++)
                {
                    commandingClient.outgoing.WriteLine(textFileToDisplay[i]);
                    if (i % 22 == 0)
                    {
                        commandingClient.outgoing.WriteLine("Press enter for more, or [q] to quit.");
                        if (commandingClient.incoming.ReadLine().ToLower() == "q")
                            break;

                    }
                }
            }
            else
            {
                commandingClient.outgoing.Write("File not found...");
            }
            commandingClient.outgoing.WriteLine("");
        }

        static void NewUser(Client commandingClient)
        {
            int newAccountNo = 1;
            string newUserAdminRights;
            bool userIsUnique = false;
            string newUsername;
            string tempValue;

            using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
            {
                dbConnection.Open();
                SqlDataReader dbReader = null;
                SqlCommand dbCommand = new SqlCommand("Insert into users (accountno, username, pass, admin, status) values (@accountno, @username, @pass, @admin, @status)", dbConnection);
                SqlCommand dbCommand2 = new SqlCommand("Select * from Users", dbConnection);
                dbReader = dbCommand2.ExecuteReader();
                while (dbReader.Read())
                {
                    int comparisonAccountNo = Convert.ToInt32(dbReader["accountno"].ToString());
                    if (comparisonAccountNo > newAccountNo)
                        newAccountNo = comparisonAccountNo;  // We want to find the highest account number in the database.
                }
                newAccountNo++;  // We found the highest account number, so increment it.
                dbCommand.Parameters.Add("@accountno", SqlDbType.Int).Value = newAccountNo;
                while (true)
                {
                    commandingClient.outgoing.Write("Please input the new username > ");
                    newUsername = commandingClient.incoming.ReadLine();
                    if (Regex.Match(newUsername, "^[a-zA-Z0-9_@#$%&,.?~-]*$").Success) // Are there any other characters we should allow?
                    {
                        break;
                    }
                    else
                    {
                        commandingClient.outgoing.WriteLine("Invalid username.");
                    }
                }
                while (!userIsUnique)
                {
                    if (GetUserInfoFromDb(newUsername)[1] != null)
                    {
                        commandingClient.outgoing.WriteLine("That username is taken. Please choose another.");
                        userIsUnique = false;
                        commandingClient.outgoing.Write("Please input the new username > ");
                        newUsername = commandingClient.incoming.ReadLine();
                    }
                    else
                    {
                        userIsUnique = true;
                    }
                }
                dbReader.Close();  // Very important!
                dbCommand.Parameters.Add("@username", SqlDbType.Text).Value = newUsername;
                while (true)
                {
                    commandingClient.outgoing.Write("Please input the new password > ");
                    tempValue = commandingClient.incoming.ReadLine();
                    if (Regex.Match(tempValue, "^[a-zA-Z0-9_@#$%&,.?~-]*$").Success) // Are there any other characters we should allow?
                    {
                        dbCommand.Parameters.Add("@pass", SqlDbType.Text).Value = tempValue;
                        commandingClient.outgoing.WriteLine("Password accepted.");
                        break;
                    }
                    else
                    {
                        commandingClient.outgoing.WriteLine("Invalid password.");
                    }
                }
                commandingClient.outgoing.Write("Will this user be an admin?[y/n]> ");
                if (commandingClient.incoming.ReadLine().ToLower() == "y")
                    newUserAdminRights = "true";
                else
                    newUserAdminRights = "false";
                dbCommand.Parameters.Add("@admin", SqlDbType.Text).Value = newUserAdminRights;
                commandingClient.outgoing.Write("Is this account active?[y/n]> ");
                if (commandingClient.incoming.ReadLine().ToLower() == "y")
                    dbCommand.Parameters.Add("@status", SqlDbType.Text).Value = "active";
                else
                    dbCommand.Parameters.Add("@status", SqlDbType.Text).Value = "inactive";

                dbCommand.ExecuteNonQuery();
            }
        }

        static void KickUser(Client commandingClient, string adjustedInput)
        {
            string userToKick = "";
            string kickMessage = "";

            userToKick = Regex.Match(adjustedInput, "^\\S*\\b{1}").Value;

            if (adjustedInput.IndexOf(" ") != -1)
            {
                for (int i = 0; i < adjustedInput.IndexOf(" "); i++)
                {
                    userToKick += adjustedInput[i];
                }
                for (int i = adjustedInput.IndexOf(" "); i < adjustedInput.Length; i++)
                {
                    kickMessage += adjustedInput[i];
                }
            }
            else
            {
                userToKick = adjustedInput;
                kickMessage = "Reason unknown.";
            }

            if (FindClientByUsername(userToKick) != null)
            {
                if (FindClientByUsername(userToKick).isAdmin)
                {
                    commandingClient.outgoing.WriteLine("\n That user is an admin. You cannot kick them.");
                }
                else
                {
                    FindClientByUsername(userToKick).outgoing.WriteLine("");
                    FindClientByUsername(userToKick).outgoing.WriteLine("You have been Kicked by " + commandingClient.clientUsername + ". Reason: " + kickMessage);
                    FindClientByUsername(userToKick).disconnectFlag = true;
                }
            }
            else
            {
                commandingClient.outgoing.WriteLine("\nUser is not online.");
            }

        }

        static void BanUser(Client commandingClient, string clientInput)
        {
            string userToBan = "";
            string additionalText = "";

            if (clientInput.Contains(' '))
            {
                // If there's a space in the input, then the first word should be the username, and the rest should be the reason for the ban
                userToBan = Regex.Match(clientInput, "^\\S*\\b{1}").Value;
                additionalText = Regex.Replace(clientInput, "^\\S*\\s{1}", string.Empty);
            }
            else
            {
                userToBan = clientInput;
            }
            if (GetUserInfoFromDb(userToBan)[1] != null)
            {
                using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
                {
                    dbConnection.Open();
                    SqlCommand dbCommand = new SqlCommand("update users set status = 'banned' where username like '" + userToBan + "'", dbConnection);
                    dbCommand.ExecuteNonQuery();
                }
                KickUser(commandingClient, clientInput);
            }
            else
            {
                commandingClient.outgoing.WriteLine("Cannot ban " + userToBan + ": That username does not exist.");
            }
        }

        static void UnBanUser(Client commandingClient, string clientInput)
        {
            string userToUnBan = "";

            if (clientInput.Contains(' '))
            {
                userToUnBan = Regex.Match(clientInput, "^\\S*\\b{1}").Value;
            }
            else
            {
                userToUnBan = clientInput;
            }
            if (GetUserInfoFromDb(userToUnBan) != null)
            {
                using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
                {
                    dbConnection.Open();
                    SqlCommand dbCommand = new SqlCommand("update users set status = 'active' where username like '" + userToUnBan + "'", dbConnection);
                    dbCommand.ExecuteNonQuery();
                }
            }
            else
            {
                commandingClient.outgoing.WriteLine("Cannot un-ban " + userToUnBan + ": That username does not exist.");
            }
        }

        public static Client FindClientByUsername(string username)
        {
            foreach (Client clientToFind in NetworkServer.clients)
            {
                if (clientToFind.clientUsername.ToLower() == username.ToLower())
                {
                    return clientToFind;
                }
            }
            return null;
        }

        public static string[] GetUserInfoFromDb(string username)
        {
            string[] userDbDetails = new string[5];
            using (SqlConnection dbConnection = new SqlConnection(NetworkServer.connectionString))
            {
                dbConnection.Open();
                SqlDataReader dbReader = null;
                SqlCommand dbCommand = new SqlCommand("select * from users where username like '" + username + "'", dbConnection);
                dbReader = dbCommand.ExecuteReader();
                while (dbReader.Read())
                {
                    userDbDetails[0] = dbReader["accountno"].ToString();
                    userDbDetails[1] = dbReader["username"].ToString();
                    userDbDetails[2] = dbReader["pass"].ToString(); // Is this secure?
                    userDbDetails[3] = dbReader["admin"].ToString();
                    userDbDetails[4] = dbReader["status"].ToString();
                }
            }
            return userDbDetails;
        }
    }
}
