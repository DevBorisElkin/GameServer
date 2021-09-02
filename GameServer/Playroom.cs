using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using static ServerCore.Server;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.Util_Server;
using System.Collections.Generic;
using static ServerCore.PlayroomManager;
using static ServerCore.PlayroomManager_MapData;

namespace ServerCore
{
    public class Playroom
    {
        public int id;

        public string name;
        public bool isPublic = true;
        public string password;
        public Map map = Map.DefaultMap;
        public MatchState matchState = MatchState.WaitingForPlayers;

        public int PlayersCurrAmount
        {
            get
            {
                if (playersInPlayroom != null) return playersInPlayroom.Count;
                else return 0;
            }
        }
        public int maxPlayers;
        public int playersToStart;
        public int killsToFinish;
        public int timeOfMatchInMinutes;

        public int totalTimeToFinishInSeconds;

        public List<Player> playersInPlayroom;

        public Playroom(int _id, string _name, bool _isPublic, string _password, Map _map, int _maxPlayers, int _minPlayersToStart, 
            int _minKillsToFinish, int _timeOfMatch)
        {
            id = _id;
            name = _name;
            isPublic = _isPublic;
            password = _password;
            map = _map;
            maxPlayers = _maxPlayers;
            playersToStart = _minPlayersToStart;
            killsToFinish = _minKillsToFinish;
            timeOfMatchInMinutes = _timeOfMatch;
            totalTimeToFinishInSeconds = TimeSpan.FromMinutes(timeOfMatchInMinutes).Seconds;

            playersInPlayroom = new List<Player>();
        }

        // returns pure scores string
        public string AddPlayer(Player player)
        {
            player.playroom = this;
            playersInPlayroom.Add(player);
            ManageMatchStart(player);

            return OnScoresChange(player);
        }
        public void SendMessageToAllPlayersInPlayroom(string message, Player excludePlayer, MessageProtocol mp = MessageProtocol.TCP)
        {
            foreach(var a in playersInPlayroom)
            {
                if (a == excludePlayer) continue;

                Util_Server.SendMessageToClient(message, a.client.ch, mp);
            }
        }
        // olny responsible for sending players' positions
        public void ManageRoom()
        {
            if(PlayersCurrAmount >= 1)
            {
                foreach(Player a in playersInPlayroom)
                {
                    // jumps amount first
                    a.CheckAndAddJumps();

                    string generatedString = GeneratePositionsDataOfAllPlayers(a);
                    if (string.IsNullOrEmpty(generatedString) || generatedString.Equals("empty"))
                        continue;
                    Util_Server.SendMessageToClient(generatedString, a.client.ch, MessageProtocol.UDP);
                }
            }
        }

        /// <summary>
        /// Returns true if last player leaves it
        /// </summary>
        public bool RemovePlayer(Client client)
        {
            SendMessageToAllPlayersInPlayroom($"{CLIENT_DISCONNECTED_FROM_THE_PLAYROOM}|{id}|{client.player.username}|{client.ch.ip}", client.player, MessageProtocol.TCP);
            playersInPlayroom.Remove(client.player);
            client.player = null;

            OnScoresChange(null);
            
            // check if we should close playroom
            if (playersInPlayroom.Count <= 0)
            {
                FinishMatch(MatchFinishReason.Discarded);
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Last player left playroom with id [{id}], closing it");
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

                    sb.Append($"{player.username},{player.client.ch.ip},{player.position.X}/{player.position.Y}/{player.position.Z}," +
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
                Console.WriteLine($"[{DateTime.Now}] "+e.ToString() + " ||| " + e.StackTrace);
            }
            int lastIndexOfDog = message.LastIndexOf('@');
            if (message.Length <= lastIndexOfDog + 1)
            {
                message = message.Remove(lastIndexOfDog, 1);
            }
            //Console.WriteLine($"Sending UDP message to all clients:\n{message}");
            return message;
        }
        public string ToNetworkString()
        {
            return $"{id}/{name}/{isPublic}/empty_password/{map}/{PlayersCurrAmount}/{maxPlayers}/{matchState}/{playersToStart}/{totalTimeToFinishInSeconds}/{killsToFinish}";
        }
        public string ToNetworkString(MatchState overrideStateForString)
        {
            return $"{id}/{name}/{isPublic}/empty_password/{map}/{PlayersCurrAmount}/{maxPlayers}/{overrideStateForString}/{playersToStart}/{totalTimeToFinishInSeconds}/{killsToFinish}";
        }

        public string OnScoresChange(Player playerToIgnore)
        {
            if (this == null) return "none"; 
            string newScoresString = GeneratePlayersScoresString();
            SendMessageToAllPlayersInPlayroom($"{PLAYERS_SCORES_IN_PLAYROOM}|"+newScoresString, playerToIgnore);
            return newScoresString;
        }

        // players_scores|data @data@data
        // {fullFataOfPlayersInThatRoom} => ip/nickname/kills/deaths@ip/nickname/kills/deaths@ip/nickname/kills/deaths
        public string GeneratePlayersScoresString()
        {
            string result = "";
            for (int i = 0; i < playersInPlayroom.Count; i++)
            {
                if(i < playersInPlayroom.Count - 1)
                {
                    result += $"{playersInPlayroom[i].client.ch.ip}/{playersInPlayroom[i].username}/" +
                        $"{playersInPlayroom[i].stats_kills}/{playersInPlayroom[i].stats_deaths}@";
                }
                else
                {
                    result += $"{playersInPlayroom[i].client.ch.ip}/{playersInPlayroom[i].username}/" +
                        $"{playersInPlayroom[i].stats_kills}/{playersInPlayroom[i].stats_deaths}";
                }
            }
            return result;
        }

        //*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*\\
        #region Managing playroom state and other interactive events

        public bool IsThisNewPlayerWillStartTheMatch()
        {
            if (PlayersCurrAmount + 1 == playersToStart) return true;
            return false;
        }

        public void ManageMatchStart(Player toIgnore)
        {
            if (!matchState.Equals(MatchState.WaitingForPlayers)) return;

            if(PlayersCurrAmount >= playersToStart)
            {
                matchState = MatchState.InGame;
                SendMessageToAllPlayersInPlayroom($"{MATCH_STARTED}|{(TimeSpan.FromMinutes(timeOfMatchInMinutes).TotalSeconds)}", toIgnore, MessageProtocol.TCP);

                foreach(Player a in playersInPlayroom)
                {
                    Vector3 newPosition = GetRandomSpawnPointByMap(map);
                    a.currentJumpsAmount = PlayroomManager.maxJumpsAmount;
                    Util_Server.SendMessageToClient($"{MATCH_STARTED_FORCE_OVERRIDE_POSITION_AND_JUMPS}|{a.currentJumpsAmount}|" +
                        $"{newPosition.X}/{newPosition.Y}/{newPosition.Z}", a.client.ch, MessageProtocol.TCP);
                }
                LaunchPlayroomTimer();
            }
        }

        void LaunchPlayroomTimer()
        {
            totalTimeToFinishInSeconds = (int)TimeSpan.FromMinutes(timeOfMatchInMinutes).TotalSeconds;
            MatchTimer = new Task(ManageTimeLeft);
            MatchTimer.Start();
        }

        public Task MatchTimer;
        void ManageTimeLeft()
        {
            while (matchState.Equals(MatchState.InGame))
            {
                totalTimeToFinishInSeconds--;
                SendMessageToAllPlayersInPlayroom($"{MATCH_TIME_REMAINING}|{totalTimeToFinishInSeconds}", null, MessageProtocol.TCP);
                if (totalTimeToFinishInSeconds <= 0)
                {
                    // finish
                    FinishMatch(MatchFinishReason.FinishedByTime);
                }
                Thread.Sleep(1000);
            }
        }

        public void CheckKillsForFinish()
        {
            Console.WriteLine("________Check Kills For finish");
            foreach(Player a in playersInPlayroom)
            {
                if(a.stats_kills >= killsToFinish)
                {
                    Console.WriteLine($"____a.stats_kills: {a.stats_kills}_killsToFinish: {killsToFinish}___Check Kills For finish");
                    FinishMatch(MatchFinishReason.FinishedByKills);
                    break;
                }
            }
        }
        void FinishMatch(MatchFinishReason finishReason)
        {
            Console.WriteLine("Finish Match");
            List<Player> winners = DefineWinnersOfTheMatch(finishReason);
            matchState = MatchState.Finished;
            if (finishReason.Equals(MatchFinishReason.Discarded) || winners == null || winners.Count == 0)
            {
                Console.WriteLine($"[{DateTime.Now}][PLAYROOM_MESSAGE]Finished match with id [{id}], finish reason [{finishReason}], -> No winners");
                SendMessageToAllPlayersInPlayroom($"{MATCH_FINISHED}|none|none|{MatchResult.Discarded}", null, MessageProtocol.TCP);
            }
            else
            {
                MatchResult matchResult;
                string winnerIP;
                string winnerNickname;
                if (winners.Count > 1)
                {
                    matchResult = MatchResult.Draw;
                    winnerIP = "none";
                    winnerNickname = "none";
                }
                else
                {
                    matchResult = MatchResult.PlayerWon;
                    winnerIP = winners[0].client.ch.ip;
                    winnerNickname = winners[0].client.userData.nickname;
                }
                SendMessageToAllPlayersInPlayroom($"{MATCH_FINISHED}|{winnerIP}|{winnerNickname}|{matchResult}", null, MessageProtocol.TCP);
            }
            DelayedClosePlayroom = new Task(ClosePlayroomWithDelay);
            DelayedClosePlayroom.Start();
        }

        static int minKillsToCountAsVictory = 3;
        List<Player> DefineWinnersOfTheMatch(MatchFinishReason finishReason)
        {
            if (finishReason.Equals(MatchFinishReason.Discarded)) return null;

            List<Player> winners = new List<Player>();

            int maxKillsInMatch = 0;
            foreach(Player a in playersInPlayroom)
                if (a.stats_kills > maxKillsInMatch) maxKillsInMatch = a.stats_kills;

            if(maxKillsInMatch <= minKillsToCountAsVictory) return null;

            foreach (Player a in playersInPlayroom)
            {
                if (a.stats_kills == maxKillsInMatch) winners.Add(a);
            }
            return winners;
        }

        int delayToClosePlayroom = 1000;
        Task DelayedClosePlayroom;
        void ClosePlayroomWithDelay()
        {
            Thread.Sleep(1000);
        }

        #endregion
    }
}
