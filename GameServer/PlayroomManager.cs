using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameServer.Server;
using static GameServer.NetworkingMessageAttributes;
using static GameServer.Util_Connection;
using System.Collections.Generic;
using System.Numerics;

namespace GameServer
{
    public static class PlayroomManager
    {
        public static List<Playroom> playrooms;

        public static void InitPlayroomManager()
        {
            Task manageRoomsTask = new Task(ManageRooms);
            manageRoomsTask.Start();
        }

        static void ManageRooms()
        {
            while (true)
            {
                while (playrooms.Count > 0)
                {
                    try
                    {
                        foreach (var a in playrooms)
                        {
                            a.ManageRoom();
                        }
                        Thread.Sleep(50);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                Thread.Sleep(500);
            }
        }

        public static void RequestFromClient_CreatePlayroom(ClientHandler ch, string _name, bool _isPublic, 
            string _password, Map _map, int _maxPlayers)
        {
            // check if he can create playroom and that new playroom does not exceed max amount
            // send negative response
            if (playrooms.Count + 1 > maximumPlayroomAmount)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Server has reached maximum amount playrooms", ch);
                return;
            }
            if(_name.Length > 15)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Name of room is too long", ch);
                return;
            }
            if(_maxPlayers > 10)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Playroom can't keep more than 10 players", ch);
                return;
            }

            ch.player = new Player(ch, ch.userData.nickname, Vector3.Zero);
            int id = GenerateRandomIdForPlayroom();
            Playroom playroom = new Playroom(id, _name, _isPublic, _password, _map, _maxPlayers, ch.player);
            playrooms.Add(playroom);

            // tell the client that he is accepted
            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|{id}", ch);
        }

        public static void RequestFromClient_EnterPlayroom(int room_id, ClientHandler ch, string roomPassword = "")
        {
            var room = FindPlayroomById(room_id);
            if (!room.isPublic)
            {
                if (!room.password.Equals(roomPassword))
                {
                    Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Wrong password", ch);
                    return;
                }
            }
            if(room.PlayersCurrAmount + 1 > room.maxPlayers)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Playroom is full", ch);
                return;
            }

            ch.player = new Player(ch, ch.userData.nickname, Vector3.Zero);
            // tell the client that he is accepted
            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|{room.id}", ch);

            // tell all other clients who are in Playroom that one client connected to it
            //SendMessageToAllClientsInPlayroom($"{CLIENT_CONNECTED_TO_THE_PLAYROOM}|{playroomNumber}|" +
            //    $"{client.player.position.X},{client.player.position.Y},{client.player.position.Z}|{nickname}|{client.ip}", MessageProtocol.TCP, client);

            Check_TurnOn_Playroom();
        }
        public static void RequestFromClient_EnterPasswordedPlayroom(int room_id, string room_password, ClientHandler ch)
        { 
        
        }

        static int GenerateRandomIdForPlayroom()
        {
            Random random = new Random();
            int randomInt = 0;
            int index = 0;
            bool numberNotUnique = true;
            while (numberNotUnique && index < 100)
            {
                numberNotUnique = false;
                randomInt = random.Next(1000, 10000);
                foreach (Playroom a in playrooms)
                {
                    if (a.id == randomInt) numberNotUnique = true;
                }
                index++;
            }
            return randomInt;
        }

        static Playroom FindPlayroomById(int id)
        {
            foreach(Playroom a in playrooms)
            {
                if (a.id == id) return a;
            }
            return null;
        }

        // _____OLD__________________________________________________________________________________________________
        static Task managingPlayRoom;
        static bool playroomActive;

        public static int maximumPlayroomAmount = 5;

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

            //Console.WriteLine($"Sending UDP message to all clients:\n{message}");
            return message;
        }
    }
}
