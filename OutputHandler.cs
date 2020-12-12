using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BRKMUD
{
    public class OutputHandler
    {
        protected static ArrayList clientsWhoDonNotWantToListen = new ArrayList();

        public static void sayHandler(string[] stringArrayToOutput, ArrayList listeningClients)
        {
            for (int i = 0; i < stringArrayToOutput.Length; i++)
            {
                foreach (Client clientToReceive in listeningClients)
                {
                    clientToReceive.outgoing.WriteLine(stringArrayToOutput[i]);
                }
            }
        }

        public OutputHandler(string[] stringArrayToOutput, ArrayList listeningClients)
        {
            for (int i = 0; i < stringArrayToOutput.Length; i++)
            {
                foreach (Client clientToReceive in listeningClients)
                {
                    clientToReceive.outgoing.WriteLine(stringArrayToOutput[i]);

                    if (i % 22 == 0)
                    {
                        clientToReceive.outgoing.WriteLine("Press enter for more, or [q] to quit.");
                        if (clientToReceive.incoming.ReadLine().ToLower() == "q")
                        {
                            clientsWhoDonNotWantToListen.Add(clientToReceive);
                        }
                    }
                }
                foreach (Client clientToStopListening in clientsWhoDonNotWantToListen)
                {
                    listeningClients.Remove(clientToStopListening);
                }
                clientsWhoDonNotWantToListen.Clear();
            }
        }

        public OutputHandler(string[] stringArrayToOutput, Client listeningClient)
        {
            for (int i = 0; i < stringArrayToOutput.Length; i++)
            {
                
                    listeningClient.outgoing.WriteLine(stringArrayToOutput[i]);

                    if (i % 22 == 0)
                    {
                        listeningClient.outgoing.WriteLine("Press enter for more, or [q] to quit.");
                        if (listeningClient.incoming.ReadLine().ToLower() == "q")
                        {
                            break;
                        }
                    }
                
            }
        }

        public OutputHandler(char charToOutput, ArrayList listeningClients)
        {
        }

        public OutputHandler(char[] charArrayToOutput, ArrayList listeningClients)
        {
        }

        public static void fileOutputHandler(string fileToOutput, ArrayList listeningClients)
        {
            if (File.Exists(fileToOutput))
            {
                string[] textFileToDisplay = File.ReadAllLines(fileToOutput);
                foreach (Client listeningClient in listeningClients)
                {
                    listeningClient.outgoing.WriteLine(textFileToDisplay[0]);  // otherwise the if check will fire on the 0th iteration
                }
                for (int i = 1; i < textFileToDisplay.Length; i++)
                {
                    foreach (Client listeningClient in listeningClients)
                    {
                        listeningClient.outgoing.WriteLine(textFileToDisplay[i]);
                        if (i % 22 == 0)
                        {
                            listeningClient.outgoing.WriteLine("Press enter for more, or [q] to quit.");
                            if (listeningClient.incoming.ReadLine().ToLower() == "q")
                                break;

                        }
                    }
                }
            }
            else
            {
                foreach (Client listeningClient in listeningClients)
                {
                    listeningClient.outgoing.Write("File not found...");
                }
            }
            foreach (Client listeningClient in listeningClients)
            {
                listeningClient.outgoing.WriteLine("");
            }
        }
    }
}
