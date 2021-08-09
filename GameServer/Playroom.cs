using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameServer.Server;
using static GameServer.NetworkingMessageAttributes;
using static GameServer.Util_Connection;
using System.Collections.Generic;

namespace GameServer
{
    public class Playroom
    {
        public int id;

        public string name;
        public bool isPublic = true;
        public string password;
        public Map map = Map.DefaultMap;

        public int PlayersCurrAmount
        {
            get
            {
                if (playersInPlayroom != null) return playersInPlayroom.Count;
                else return 0;
            }
        }
        public int maxPlayers;


        public List<Player> playersInPlayroom;


        public Playroom(int _id, string _name, bool _isPublic, string _password, Map _map, int _maxPlayers)
        {
            id = _id;
            name = _name;
            isPublic = _isPublic;
            password = _password;
            map = _map;
            maxPlayers = _maxPlayers;

            playersInPlayroom = new List<Player>();
        }

        public void AddPlayer(Player player)
        {
            player.playroom = this;
            playersInPlayroom.Add(player);

            SendMessageToAllPlayersInPlayroom($"{CLIENT_CONNECTED_TO_THE_PLAYROOM}|{id}|" +
                $"{player.position.X},{player.position.Y},{player.position.Z}|{player.username}|{player.ch.ip}", player);
        }

        void SendMessageToAllPlayersInPlayroom(string message, Player excludePlayer, MessageProtocol mp = MessageProtocol.TCP)
        {
            foreach(var a in playersInPlayroom)
            {
                if (a == excludePlayer) continue;

                Util_Server.SendMessageToClient(message, a.ch, mp);
            }
        }

        public void ManageRoom()
        {
            if(PlayersCurrAmount > 1)
            {
                foreach(Player a in playersInPlayroom)
                {
                    string generatedString = GeneratePositionsDataOfAllPlayers(a);
                    if (string.IsNullOrEmpty(generatedString) || generatedString.Equals("empty"))
                        continue;
                    a.SendPositionsOfOtherPlayers(generatedString);
                }
            }
        }

        public void PerformPlayerAction()
        {

        }

        /// <summary>
        /// Returns true if last player leaves it
        /// </summary>
        public bool RemovePlayer(ClientHandler ch)
        {
            SendMessageToAllPlayersInPlayroom($"{CLIENT_DISCONNECTED_FROM_THE_PLAYROOM}|{id}|{ch.player.username}|{ch.ip}", ch.player, MessageProtocol.TCP);
            playersInPlayroom.Remove(ch.player);
            ch.player = null;

            // check if we should close playroom

            if (playersInPlayroom.Count <= 0)
            {
                Console.WriteLine($"[SERVER_MESSAGE]: Last player left playroom with id [{id}], closing it");
                return true;
            }
            else return false;

        }

        public string GeneratePositionsDataOfAllPlayers(Player excludePlayer)
        {
            string message = "";
            try
            {
                StringBuilder sb = new StringBuilder();
                // "players_positions_in_playroom|nickname,ip,position,rotation@nickname,ip,position,rotation@enc..."
                sb.Append(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|");
                foreach (var player in playersInPlayroom)
                {
                    if (player == excludePlayer) continue;

                    sb.Append($"{player.username},{player.ch.ip},{player.position.X}/{player.position.Y}/{player.position.Z}," +
                        $"{player.rotation.X}/{player.rotation.Y}/{player.rotation.Z}@");
                }
                message = sb.ToString();
                if (message.Equals(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|"))
                {
                    if (message.Equals(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|")) return "empty";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + " ||| " + e.StackTrace);
            }
            int lastIndexOfDog = message.LastIndexOf('@');
            if (message.Length <= lastIndexOfDog + 1)
            {
                message = message.Remove(lastIndexOfDog, 1);
            }
            //Console.WriteLine($"Sending UDP message to all clients:\n{message}");
            return message;
        }
        // "playrooms_data_response|playroom_data(/),playroom_data, playroom_data"
        // data: id/nameOfRoom/is_public/password/map/currentPlayers/maxPlayers
        public string ToNetworkString()
        {
            return $"{id}/{name}/{isPublic}/empty_password/{map}/{PlayersCurrAmount}/{maxPlayers}";
        }
    }

    public enum Map { DefaultMap }
}
