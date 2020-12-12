using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BRKMUD
{
    class ChatServer
    {
        public static ArrayList sayChannel = new ArrayList();  //This channel is for players who are in the same room.
        public static ArrayList globalChannel = new ArrayList();  //Add clients to this list when they join the channel, remove them when they leave.

        public static void Chat(Client chattyClient, string clientInput, string channel)  // This should be self-explanatory.
        {
            chattyClient.outgoing.WriteLine("");
            switch (channel)
            {
                case "Say":
                    if (sayChannel.IndexOf(chattyClient) == -1)  // This is deprecated since say is now limited to occupants of the same room. Not sure if this will be useful in the future, though, so I'm leaving it in for now.
                        sayChannel.Add(chattyClient);

                    LogHandler.ChatLogger(chattyClient, DateTime.Now, clientInput, channel);  //This needs to be re-written to be more selective. Clients should not be able to see the history of all chat everywhere on the server. Perhaps the chatlogger should be part of the Client? That might make it easier to limit the history to what the client has actually seen.

                    foreach (Client sayClient in chattyClient.currentRoom.occupants)
                    {
                        sayClient.outgoing.WriteLine("");  // For formatting purposes. This is crude, I'll tighten it up later.
                        sayClient.outgoing.WriteLine(chattyClient.clientUsername + " says: \"" + clientInput + "\"");
                    }
                    break;
                case "global":
                    if (globalChannel.IndexOf(chattyClient) == -1)  // Auto-add client to the chat channel, if necessary.
                        globalChannel.Add(chattyClient);

                    LogHandler.ChatLogger(chattyClient, DateTime.Now, clientInput, channel);

                    foreach (Client globalClient in globalChannel)
                    {
                        globalClient.outgoing.WriteLine(chattyClient.clientUsername + " says: \"" + clientInput + "\"");
                    }

                    break;
                default:
                    break;
            }
        }

        public static void Tell(Client teller, string toBeTold, string tellContent)
        {
            Client tellReceiver = InputHandler.FindClientByUsername(toBeTold);
            if (tellReceiver != null)
            {
                teller.outgoing.WriteLine("You say: " + tellContent);
                tellReceiver.outgoing.WriteLine(teller.clientUsername + " says: " + tellContent);
            }
            else
            {
                teller.outgoing.WriteLine("User " + toBeTold + " is not currently online.");
            }
        }
    }
}
