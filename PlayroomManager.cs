using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameServer.Server;
using static GameServer.NetworkingMessageAttributes;
using static GameServer.Util_Connection;

namespace GameServer
{
    public static class PlayroomManager
    {
        static Task managingPlayRoom;
        static bool playroomActive;

        public static void InitPlayroom()
        {
            Util_Server.OnClientDisconnectedEvent += OnClientDisconnected;
        }

        static void OnClientDisconnected(ClientHandler ch, string error)
        {
            if (ch.player == null) return;
            Util_Server.SendMessageToAllClientsInPlayroom($"{CLIENT_DISCONNECTED_FROM_THE_PLAYROOM}|{1}|{ch.player.username}|{ch.ip}", MessageProtocol.TCP, ch);
            ch.player = null;
            Check_TurnOff_Playroom();
        }


        public static void Check_TurnOn_Playroom()
        {
            int playersInPlayRoom = 0;
            foreach (var a in clients.Values)
            {
                if (a.player != null) playersInPlayRoom++;
            }
            if (playersInPlayRoom <= 0)
            {
                Console.WriteLine($"[SERVER_ERROR]: Not enough players in play room to turn on Managing of it. [{playersInPlayRoom}]");
                return;
            }
            if (!playroomActive) Console.WriteLine("Opening playroom. First player joined it.");

            playroomActive = true;
            managingPlayRoom = new Task(ManagePlayroom);
            managingPlayRoom.Start();
        }
        public static void Check_TurnOff_Playroom()
        {
            int playersInPlayRoom = 0;
            foreach (var a in clients.Values)
            {
                if (a.player != null) playersInPlayRoom++;
            }
            if (playersInPlayRoom <= 0)
            {
                if (playroomActive) Console.WriteLine("Closing playroom. Last player left it.");
                playroomActive = false;
            }

        }

        static void ManagePlayroom()
        {
            // here we will send room's position and rotation to all players connected to that room

            while (playroomActive) // sending data 10 times a second
            {
                try
                {
                    foreach (var key in clients.Keys)
                    {
                        clients.TryGetValue(key, out ClientHandler ch);

                        if (ch == null || ch.player == null) 
                            continue;

                        string generatedString = GenerateStringSendingPlayersOtherPlayersPositions(ch);
                        if (!generatedString.Equals("empty"))
                            Util_Server.SendMessageToClient(generatedString, ch, MessageProtocol.UDP);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} ||| + {e.StackTrace}");
                }
                Thread.Sleep(50);
            }


        }

        // |nickname,ip,position,rotation@nickname,ip,position,rotation@enc..."
        static string GenerateStringSendingPlayersOtherPlayersPositions(ClientHandler exceptThisOne)
        {
            string message = "";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|");
                foreach (var a in clients.Values)
                {
                    if (a.player == null) continue;
                    if (a == exceptThisOne) continue;

                    sb.Append($"{a.player.username},{a.ip},{a.player.position.X}/{a.player.position.Y}/{a.player.position.Z}," +
                        $"{a.player.rotation.X}/{a.player.rotation.Y}/{a.player.rotation.Z}@");
                }
                message = sb.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + " ||| " + e.StackTrace);
            }
            int lastIndexOfDog = message.LastIndexOf('@');
            if (message.Length > lastIndexOfDog + 1)
            {
                // we can leave dog like that
            }
            else
            {
                message = message.Remove(lastIndexOfDog, 1);
            }

            if (message.Equals(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|")) return "empty";

            Console.WriteLine($"Sending UDP message to all clients:\n{message}");
            return message;
        }
    }
}
