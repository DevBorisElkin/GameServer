using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameServer.Server;
using static GameServer.Util_Connection;
using static GameServer.NetworkingMessageAttributes;
using System.Globalization;
using System.Numerics;

namespace GameServer
{
    public static class Util_PlayroomManager
    {

        public static void ParceMessage_Playroom(string message, ClientHandler ch)
        {
            try
            {
                if (message.StartsWith(PLAYROOMS_DATA_REQUEST))
                {
                    PlayroomManager.RequestFromClient_GetPlayroomsData(ch);
                }
                //          0           1          2         3     4      5
                // "create_playroom|nameOfRoom|is_public|password|map|maxPlayers";
                else if (message.StartsWith(CREATE_PLAY_ROOM))
                {
                    string[] substrings = message.Split("|");

                    bool.TryParse(substrings[2], out bool isPublic);
                    Enum.TryParse(substrings[4], out Map map);

                    PlayroomManager.RequestFromClient_CreatePlayroom(ch, substrings[1], isPublic,
                        substrings[3], map, Int32.Parse(substrings[5]));
                }
                // WILL REMAKE RESPONSE 'CONFIRM_ENTER_PLAYROOM' and there will be response: okay, or error
                // "enter_playroom|3251|the_greatest_password_ever";
                else if (message.StartsWith(ENTER_PLAY_ROOM))
                // normally here should be some logic, checking, if specific playroom has space for new players to join
                {
                    string[] substrings = message.Split("|");

                    Console.WriteLine($"[{ch.id}][{ch.ip}]Client requested to connect to playroom");
                    if(substrings.Length == 2)
                        PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), ch);
                    else if(substrings.Length == 3)
                        PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), ch, substrings[2]);
                }
                else if (message.StartsWith(CLIENT_SHARES_PLAYROOM_POSITION))
                {
                    string[] substrings = message.Split("|");
                    string[] positions = substrings[1].Split("/");
                    Vector3 position = new Vector3(
                        float.Parse(positions[0], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(positions[1], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(positions[2], CultureInfo.InvariantCulture.NumberFormat));

                    string[] rotations = substrings[2].Split("/");
                    Quaternion rotation = new Quaternion(
                        float.Parse(rotations[0], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(rotations[1], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(rotations[2], CultureInfo.InvariantCulture.NumberFormat),
                        0);

                    PlayroomManager.RequestFromClient_StorePlayerPositionAndRotation(ch, position, rotation);
                }
                else if (message.StartsWith(CLIENT_DISCONNECTED_FROM_THE_PLAYROOM))
                {
                    Console.WriteLine($"[SERVER_MESSAGE]:Client [{ch.id}][{ch.ip}] disconnected from playroom");
                    string[] substrings = message.Split("|");
                    PlayroomManager.RequestFromClient_DisconnectFromPlayroom(int.Parse(substrings[1]), ch);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
