using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace BRKMUD
{

    struct EXIT  // No idea if this is better or worse than using a class.
    {
        public string exitName;
        public string exitDescription;
        public string exitDestination;
    }
    public class room  // I plan no adding methods to this, so it's going to be a class.
    {
        public string roomName;
        public string description;
        public string altDescription;
        public ArrayList exits = new ArrayList();
        public ArrayList occupants = new ArrayList();
        public ArrayList residentActors = new ArrayList();  // "actors" are any interACTive object that is not a player (mobs, npcs, interactive room decor such as chests, chairs, paintings, etc)
    }

    class RoomHandler
    {
        public static void readRoom(Client commmandingClient, string roomToRead)  //This is for testing purposes. Remove when no longer necessary.
        {
            commmandingClient.outgoing.WriteLine("Outputting: ");
            XmlTextReader reader = new XmlTextReader(roomToRead);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        commmandingClient.outgoing.WriteLine(reader.Name);
                        if (reader.HasAttributes)
                        {
                            commmandingClient.outgoing.WriteLine(reader.GetAttribute("name"));
                        }
                        break;
                    case XmlNodeType.Text:
                        commmandingClient.outgoing.WriteLine(reader.Value);
                        break;
                    default:
                        break;
                }
            }
        }

        public static void loadAllRooms()  // load ALL the rooms!!
        {
            try
            {

                foreach (string fileName in Directory.GetFiles("rooms", "*.room"))  // Every file in the room/ directory that has the .room extension is a room. I'm thinking that rooms should be separated into subfolders based on arrangement/relevant story/quests. That would require some adjustments to this method.
                {                                                                   // I'm thinking about making a .map file for each room group. The map file would define story goals, mobs confined to the story area, exits to other maps, etc.
                    room tempRoom = new room();                                     // In fact, should there be an overmap? The overmap would contain the non-story rooms you inhabit when travelling between maps...

                    XmlTextReader reader = new XmlTextReader(fileName);
                    while (reader.Read())
                    {
                        switch (reader.Name)
                        {
                            case "RoomData":
                                if (reader.HasAttributes)
                                    tempRoom.roomName = reader.GetAttribute("name");
                                break;
                            case "Description":
                                if (reader.GetAttribute("name") == "RoomDescription")
                                {
                                    tempRoom.description = reader.ReadString();
                                }
                                else
                                {
                                    tempRoom.altDescription = reader.ReadString();
                                }
                                break;
                            case "EXIT":
                                EXIT tempExit = new EXIT();
                                tempExit.exitName = reader.GetAttribute("name");
                                tempExit.exitDestination = reader.GetAttribute("leadsto");
                                tempExit.exitDescription = reader.GetAttribute("description");
                                tempRoom.exits.Add(tempExit);
                                break;
                            default:
                                break;
                        }

                    }

                    NetworkServer.roomArray.Add(tempRoom);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LogHandler.LogAServerEvent(e.ToString());
            }
        }
        public static void showLoadedRooms( Client commandingClient)  // Also for testing purposes. Not sure if this will be necessary in the final release.
        {
            Console.WriteLine("Loaded rooms: ");
            foreach (room file in NetworkServer.roomArray)
            {
                Console.WriteLine("Name: "+file.roomName);
                Console.WriteLine("Description: "+file.description);
                Console.WriteLine("Alt Desc: "+file.altDescription);
                foreach (EXIT exit in file.exits)
                {
                    Console.WriteLine("Exit: "+exit.exitName+" - "+exit.exitDestination);
                }
            }
            Console.WriteLine("End of the list.");
        }

        public static room findRoom(string roomToFind)  // For finding rooms...
        {
            foreach (room queryRoom in NetworkServer.roomArray)
            {
                if (queryRoom.roomName.ToLower() == roomToFind.ToLower())
                    return queryRoom;
            }
            return null;
        }
        /* **********DEPRECATED**********
        public static bool roomExists(string roomToCheck)  // This might be redundant. Mayhap we can use the findRoom method for this purpose.
        {
            foreach (room queryRoom in NetworkServer.roomArray)
            {
                if (queryRoom.roomName == roomToCheck)
                    return true;
            }
            return false;
        }
        */
        public static void changeRoom(Client roomChangeClient, room newRoom) // Here we will update chat channels, room occupant arrays, etc.
        {
            roomChangeClient.onRoomChange(roomChangeClient, newRoom);
        }
    }
}
