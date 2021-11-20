using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ServerCore.NetworkingMessageAttributes;
using System.Collections.Generic;
using System.Numerics;
using static ServerCore.PlayroomManager_MapData;
using static ServerCore.Util_Server;

namespace ServerCore
{
    public static class PlayroomManager
    {
        public static List<Playroom> playrooms = new List<Playroom>();
        static int maximumPlayroomAmount = 5;

        public const float reloadTime = 1.4f;
        public const float jumpCooldownTime = 25f;
        public const int maxJumpsAmount = 5;

        public const int minRandomAmountOfRuneJumps = 1;
        public const int maxRandomAmountOfRuneJumps = 3;

        public static Vector2 randomRuneSpawnTime = new Vector2(20f, 85f);
        //public static Vector2 randomRuneSpawnTime = new Vector2(5f, 5f);

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
                            if(a != null) a.ManageRoom();
                        }
                        Thread.Sleep(20);  // old was 50 [20 times per second], now 20 [50 times per second]
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

        public static void RequestFromClient_CreatePlayroom(Client client, string _name, bool _isPublic, string _password, Map _map, int _maxPlayers,
            int _playersToStart, int _killsToFinish, int _timeOfMatch)
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

            if (_playersToStart > _maxPlayers)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|'Max players' can't be less than 'players to start the match'", client.ch);
                return;
            }
            if (_playersToStart < 2||_playersToStart > 10 || _killsToFinish < 5 || _killsToFinish > 50 || _timeOfMatch < 3 || _timeOfMatch > 30)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Wrong parameters in additional settings", client.ch);
                return;
            }
            if (!UDP.TryToRetrieveEndPoint(client.ch))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Client has no UDP end point assigned", client.ch);
                return;
            }

            int playroomID = GenerateRandomIdForPlayroom();
            Playroom playroom = new Playroom(playroomID, _name, _isPublic, _password, _map, _maxPlayers, _playersToStart, _killsToFinish, _timeOfMatch);
            client.player = new Player(client, Vector3.Zero);
            
            string scoresString = playroom.AddPlayer(client.player, out Vector3 fakeSpawnPos);
            playrooms.Add(playroom);
            Vector3 spawnPos = GetRandomSpawnPointByMap(_map);

            Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Client [{client.ch.ip}] requested to create playroom and his request was accepted");
            // tell the client that he is accepted

            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|" +
                $"{playroom.ToNetworkString()}|{scoresString}|{maxJumpsAmount}|{spawnPos.X}/{spawnPos.Y}/{spawnPos.Z}", client.ch);
        }

        public static void RequestFromClient_EnterPlayroom(int room_id, Client client, string roomPassword = "")
        {
            var playroom = FindPlayroomById(room_id);
            if(playroom == null)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Didn't find playroom with id {room_id}", client.ch);
                return;
            }
            if (!playroom.isPublic)
            {
                if (!playroom.password.Equals(roomPassword))
                {
                    Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Wrong password", client.ch);
                    return;
                }
            }
            if(playroom.PlayersCurrAmount + 1 > playroom.maxPlayers)
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Playroom is full", client.ch);
                return;
            }
            if (!UDP.TryToRetrieveEndPoint(client.ch))
            {
                Util_Server.SendMessageToClient($"{REJECT_ENTER_PLAY_ROOM}|Client has no UDP end point assigned", client.ch);
                return;
            }

            client.player = new Player(client, Vector3.Zero);
            MatchState matchState;
            if (playroom.IsThisNewPlayerWillStartTheMatch()) matchState = MatchState.InGame;
            else matchState = MatchState.WaitingForPlayers;

            string scoresString = playroom.AddPlayer(client.player, out Vector3 spawnPos);

            playroom.SendMessageToAllPlayersInPlayroom($"{CLIENT_CONNECTED_TO_THE_PLAYROOM}|{client.ch.ip}|{client.userData.nickname}", client.player, Util_Server.MessageProtocol.TCP);

            Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Client [{client.ch.ip}] requested to enter playroom [{room_id}] and his request was accepted");
            Util_Server.SendMessageToClient($"{CONFIRM_ENTER_PLAY_ROOM}|" +
                $"{playroom.ToNetworkString(matchState)}|{scoresString}|{maxJumpsAmount}|{spawnPos.X}/{spawnPos.Y}/{spawnPos.Z}", client.ch);

            Runes_MessagingManager.NotifyNewlyConnectedPlayerOfExistingRunes(client.player, playroom);
            Runes_MessagingManager.NotifyNewlyConnectedPlayerOfPlayersRuneEffects(client.player, playroom);
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

            if (client.player.playroom.playroomID != playroomId)
                Console.WriteLine($"[{DateTime.Now}][SERVER ERROR]: playroom id of player message and assigned playroom's id are not the same: {client.player.playroom.playroomID} | {playroomId}");

            Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Client [{client.userData.db_id}][{client.ch.ip}] notified about leaving playroom [{playroomId}]");
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
                    if (a.playroomID == randomInt) numberNotUnique = true;
                }
                index++;
            }
            return randomInt;
        }

        static Playroom FindPlayroomById(int id)
        {
            foreach(Playroom a in playrooms)
            {
                if (a.playroomID == id) return a;
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
        
        public static void ClosePlayroom(Playroom room)
        {
            foreach(Player a in room.playersInPlayroom)
            {
                a.client.player = null;
            }
            room.playersInPlayroom = null;

            playrooms.Remove(room);
            room = null;
        }
    }
}
