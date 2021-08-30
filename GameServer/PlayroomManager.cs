using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ServerCore.NetworkingMessageAttributes;
using System.Collections.Generic;
using System.Numerics;
using static ServerCore.PlayroomManager_MapData;

namespace ServerCore
{
    public static class PlayroomManager
    {
        public static List<Playroom> playrooms = new List<Playroom>();
        static int maximumPlayroomAmount = 5;

        public const float reloadTime = 1.4f;
        public const float jumpCooldownTime = 25f;
        public const int maxJumpsAmount = 5;

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
                        Console.WriteLine($"[{DateTime.Now}] " +e.ToString());
                    }
                }
                Thread.Sleep(500);
            }
        }
        public static void RequestFromClient_GetPlayroomsData(Client client)
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
                Util_Server.SendMessageToClient(result, client.ch);
            }
            catch(Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] " + e.ToString());
            }
        }

        public static void RequestFromClient_CreatePlayroom(Client client, string _name, bool _isPublic, 
            string _password, Map _map, int _maxPlayers)
        {
            // check if he can create playroom and that new playroom does not exceed max amount
            // send negative response
            if (playrooms.Count + 1 > maximumPlayroomAmount)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Server has reached maximum amount playrooms", client.ch);
                return;
            }
            if (_name.Length < 5)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Name of room is too short", client.ch);
                return;
            }
            if (_name.Length > 20)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Name of room is too long", client.ch);
                return;
            }
            if (_maxPlayers > 10)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Playroom can't keep more than 10 players", client.ch);
                return;
            }
            if (!_isPublic && (_password.Length < 5 || _password.Length > 15))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Password length is wrong", client.ch);
                return;
            }
            if (!UDP.TryToRetrieveEndPoint(client.ch))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Client has no UDP end point assigned", client.ch);
                return;
            }

            int id = GenerateRandomIdForPlayroom();
            Playroom playroom = new Playroom(id, _name, _isPublic, _password, _map, _maxPlayers);
            client.player = new Player(client, client.userData.nickname, Vector3.Zero);
            string scoresString = playroom.AddPlayer(client.player);
            playrooms.Add(playroom);
            Vector3 spawnPos = GetRandomSpawnPointByMap(_map);

            Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Client [{client.ch.ip}] requested to create playroom and his request was accepted");
            // tell the client that he is accepted

            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|" +
                $"{playroom.ToNetworkString()}|{scoresString}|{maxJumpsAmount}|{spawnPos.X}/{spawnPos.Y}/{spawnPos.Z}", client.ch);
        }

        public static void RequestFromClient_EnterPlayroom(int room_id, Client client, string roomPassword = "")
        {
            var room = FindPlayroomById(room_id);
            if(room == null)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Didn't find playroom with id {room_id}", client.ch);
                return;
            }
            if (!room.isPublic)
            {
                if (!room.password.Equals(roomPassword))
                {
                    Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Wrong password", client.ch);
                    return;
                }
            }
            if(room.PlayersCurrAmount + 1 > room.maxPlayers)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Playroom is full", client.ch);
                return;
            }
            if (!UDP.TryToRetrieveEndPoint(client.ch))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Client has no UDP end point assigned", client.ch);
                return;
            }

            client.player = new Player(client, client.userData.nickname, Vector3.Zero);
            string scoresString = room.AddPlayer(client.player);
            Vector3 spawnPos = GetRandomSpawnPointByMap(room.map);

            Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Client [{client.ch.ip}] requested to enter playroom [{room_id}] and his request was accepted");
            // tell the client that he is accepted
            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|{room.ToNetworkString()}|{scoresString}|" +
                $"{maxJumpsAmount}|{spawnPos.X}/{spawnPos.Y}/{spawnPos.Z}", client.ch);
        }
        public static void RequestFromClient_StorePlayerPositionAndRotation(Client client, Vector3 _position, Quaternion _rotation)
        {
            if (client.player != null) // && client.player.isAlive)
            {
                client.player.position = _position;
                client.player.rotation = _rotation;
            }
        }

        public static void RequestFromClient_DisconnectFromPlayroom(int playroomId, Client client)
        {
            if (client.player == null) return;
            if (client.player.playroom == null) return;

            Playroom playroom = client.player.playroom;

            if (client.player.playroom.id != playroomId)
                Console.WriteLine($"[{DateTime.Now}][SERVER ERROR]: playroom id of player message and assigned playroom's id are not the same: {client.player.playroom.id} | {playroomId}");

            Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Client [{client.ch.ip}] notified about leaving playroom [{playroomId}]");
            if(client.player.playroom.RemovePlayer(client)) ClosePlayroom(playroom);
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
            Client assignedClient = Client.GetClientByClientHandler(ch);
            if (assignedClient == null) { Console.WriteLine($"[{DateTime.Now}]Error, didn't find client by client handler in Playroom Manager"); return; }
            if (assignedClient.player == null) return;

            if (assignedClient.player.playroom == null) return;

            Playroom playroom = assignedClient.player.playroom;
            if(assignedClient.player.playroom.RemovePlayer(assignedClient)) ClosePlayroom(playroom);
            
        }
        
        static void ClosePlayroom(Playroom room)
        {
            playrooms.Remove(room);
            room = null;
        }
    }
}
