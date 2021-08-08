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


        public Playroom(int _id, string _name, bool _isPublic, string _password, Map _map, int _maxPlayers, Player creatorOfRoom)
        {
            id = _id;
            name = _name;
            isPublic = _isPublic;
            password = _password;
            map = _map;
            maxPlayers = _maxPlayers;

            playersInPlayroom = new List<Player>();
            AddPlayer(creatorOfRoom);
        }

        public void AddPlayer(Player player)
        {
            if(PlayersCurrAmount + 1 <= maxPlayers)
            {
                playersInPlayroom.Add(player);

            }
            else
            {

            }
        }

        public void ManageRoom()
        {
            if(PlayersCurrAmount > 1)
            {
                foreach(Player a in playersInPlayroom)
                {
                    a.SendPositionsOfOtherPlayers(GeneratePositionsDataOfAllPlayers(a));
                }
            }
        }

        public void PerformPlayerAction()
        {

        }

        public void RemovePlayer()
        {

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
            if (message.Length < lastIndexOfDog + 1)
            {
                message = message.Remove(lastIndexOfDog, 1);
            }
            //Console.WriteLine($"Sending UDP message to all clients:\n{message}");
            return message;
        }


    }

    public enum Map { DefaultMap }
}
