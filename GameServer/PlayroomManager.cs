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
        public static List<Playroom> playrooms = new List<Playroom>();
        static int maximumPlayroomAmount = 5;

        public static void InitPlayroomManager()
        {
            Task manageRoomsTask = new Task(ManageRooms);
            manageRoomsTask.Start();

            Util_Server.OnClientDisconnectedEvent += OnClientDisconnected;
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
        // "playrooms_data_response|playroom_data(/),playroom_data, playroom_data"
        // data: id/nameOfRoom/is_public/password/map/currentPlayers/maxPlayers
        public static void RequestFromClient_GetPlayroomsData(ClientHandler ch)
        {
            try
            {
                string result = PLAYROOMS_DATA_RESPONSE + "|";
                for (int i = 0; i < playrooms.Count; i++)
                {
                    if (i < playrooms.Count - 1)
                        result += playrooms[i].ToNetworkString() + ",";
                    else
                        result += playrooms[i].ToNetworkString();
                }
                Util_Server.SendMessageToClient(result, ch);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
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
            if (_name.Length < 5)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Name of room is too short", ch);
                return;
            }
            if (_name.Length > 20)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Name of room is too long", ch);
                return;
            }
            if (_maxPlayers > 10)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Playroom can't keep more than 10 players", ch);
                return;
            }
            if (!_isPublic && (_password.Length < 5 || _password.Length > 15))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Password length is wrong", ch);
                return;
            }
            if (!UDP.TryToRetrieveEndPoint(ch))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Client has no UDP end point assigned", ch);
                return;
            }

            int id = GenerateRandomIdForPlayroom();
            Playroom playroom = new Playroom(id, _name, _isPublic, _password, _map, _maxPlayers);
            ch.player = new Player(ch, ch.userData.nickname, Vector3.Zero);
            playroom.AddPlayer(ch.player);
            playrooms.Add(playroom);

            Console.WriteLine($"[SERVER_MESSAGE]: Client [{ch.ip}] requested to create playroom and his request was accepted");
            // tell the client that he is accepted
            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|{playroom.ToNetworkString()}", ch);
        }

        public static void RequestFromClient_EnterPlayroom(int room_id, ClientHandler ch, string roomPassword = "")
        {
            var room = FindPlayroomById(room_id);
            if(room == null)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Didn't find playroom with id {room_id}", ch);
                return;
            }
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
            if (!UDP.TryToRetrieveEndPoint(ch))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Client has no UDP end point assigned", ch);
                return;
            }

            ch.player = new Player(ch, ch.userData.nickname, Vector3.Zero);
            room.AddPlayer(ch.player);

            Console.WriteLine($"[SERVER_MESSAGE]: Client [{ch.ip}] requested to enter playroom [{room_id}] and his request was accepted");
            // tell the client that he is accepted
            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|{room.ToNetworkString()}", ch);
        }
        public static void RequestFromClient_StorePlayerPositionAndRotation(ClientHandler client, Vector3 _position, Quaternion _rotation)
        {
            if (client.player != null)
            {
                client.player.position = _position;
                client.player.rotation = _rotation;
            }
        }
        public static void RequestFromClient_DisconnectFromPlayroom(int playroomId, ClientHandler ch)
        {
            if (ch.player == null) return;
            if (ch.player.playroom == null) return;

            Playroom playroom = ch.player.playroom;

            if (ch.player.playroom.id != playroomId)
                Console.WriteLine($"[SERVER ERROR]: playroom id of player message and assigned playroom's id are not the same: {ch.player.playroom.id} | {playroomId}");

            Console.WriteLine($"[SERVER_MESSAGE]: Client [{ch.ip}] notified about leaving playroom [{playroomId}]");
            bool shouldClose = ch.player.playroom.RemovePlayer(ch);
            CheckAndClosePlayroom(playroom, shouldClose);
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

        static void OnClientDisconnected(ClientHandler ch, string error)
        {
            if (ch.player == null) return;

            if (ch.player.playroom == null) return;

            Playroom playroom = ch.player.playroom;
            bool shouldClose = ch.player.playroom.RemovePlayer(ch);
            CheckAndClosePlayroom(playroom, shouldClose);
        }
        
        static void CheckAndClosePlayroom(Playroom room, bool shouldClose)
        {
            if (shouldClose)
            {
                playrooms.Remove(room);
                room = null;
            }
        }

    }
}
