using System;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using static ServerCore.Server;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.Util_Server;
using System.Collections.Generic;
using static ServerCore.PlayroomManager;
using static ServerCore.PlayroomManager_MapData;
using static ServerCore.PlayroomManager_Runes;
using static ServerCore.DataTypes;
using DatabaseAccess;
using System.Text;

namespace ServerCore
{
    public class Playroom
    {
        public int playroomID;

        public string name;
        public bool isPublic = true;
        public string password;
        public Map map = Map.DefaultMap;
        public MatchState matchState = MatchState.WaitingForPlayers;

        public PlayroomManager_Runes runesManager;

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
        int totalTimeToFinishInSecUnchanged;

        public List<Player> playersInPlayroom;

        public Playroom(int _playrooomID, string _name, bool _isPublic, string _password, Map _map, int _maxPlayers, int _minPlayersToStart, 
            int _minKillsToFinish, int _timeOfMatch)
        {
            playroomID = _playrooomID;
            name = _name;
            isPublic = _isPublic;
            password = _password;
            map = _map;
            maxPlayers = _maxPlayers;
            playersToStart = _minPlayersToStart;
            killsToFinish = _minKillsToFinish;
            timeOfMatchInMinutes = _timeOfMatch;
            totalTimeToFinishInSeconds = (int)TimeSpan.FromMinutes(timeOfMatchInMinutes).TotalSeconds + totalMatchStartTime;
            totalTimeToFinishInSecUnchanged = totalTimeToFinishInSeconds;

            playersInPlayroom = new List<Player>();

            runesManager = new PlayroomManager_Runes();
            runesManager.Init(this);
        }

        // returns pure scores string
        public string AddPlayer(Player player, out Vector3 playerSpawnPos)
        {
            player.playroom = this;
            playersInPlayroom.Add(player);
            ManageMatchStart(player, out playerSpawnPos);

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
            if (playersInPlayroom == null || playersInPlayroom.Count == 0) return;
            if(PlayersCurrAmount >= 1)
            {
                foreach(Player a in playersInPlayroom)
                {
                    // jumps amount first
                    a.CheckAndAddJumps();
                    if (matchState == MatchState.InGame) a.modifiersManager.CheckRuneEffectsExpiration();

                    ManagePlayersPositions(a);
                }
            }

            if(matchState == MatchState.InGame || matchState == MatchState.JustStarting)
            {
                runesManager.Update();
            }
        }

        void ManagePlayersPositions(Player a)
        {
            string generatedString = GeneratePositionsDataOfAllPlayers();
            if (string.IsNullOrEmpty(generatedString) || generatedString.Equals("none"))
                return;

            Util_Server.SendMessageToClient(generatedString, a.client.ch, MessageProtocol.UDP);
        }

        /// <summary>
        /// Returns true if last player leaves it
        /// </summary>
        public bool RemovePlayer(Client client)
        {
            SendMessageToAllPlayersInPlayroom($"{CLIENT_DISCONNECTED_FROM_THE_PLAYROOM}|{playroomID}|{client.userData.nickname}|{client.userData.db_id}", client.player, MessageProtocol.TCP);
            playersInPlayroom.Remove(client.player);
            client.player = null;

            OnScoresChange(null);
            
            // check if we should close playroom
            if (playersInPlayroom.Count <= 0)
            {
                FinishMatch(MatchFinishReason.Discarded);
                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Last player left playroom with id [{playroomID}], closing it");
                return true;
            }
            else return false;

        }

        public string GeneratePositionsDataOfAllPlayers(Player excludePlayer = null)
        {
            string message = "";
            try
            {
                var sb = new System.Text.StringBuilder();
                // "players_positions_in_playroom|nickname,db_id,position,rotation@nickname,db_id,position,rotation@enc..."
                sb.Append(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|");
                foreach (var player in playersInPlayroom)
                {
                    if (player == excludePlayer) continue;

                    sb.Append($"{player.client.userData.nickname},{player.client.userData.db_id},{player.position.X}/{player.position.Y}/{player.position.Z}," +
                        $"{player.rotation.X}/{player.rotation.Y}/{player.rotation.Z}@");
                }
                message = sb.ToString();
                if (message.Equals(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|"))
                    return "none";
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
            return message;
        }
        public string ToNetworkString()
        {
            return $"{playroomID}/{name}/{isPublic}/empty_password/{map}/{PlayersCurrAmount}/{maxPlayers}/{matchState}/{playersToStart}/{totalTimeToFinishInSeconds}/{totalTimeToFinishInSecUnchanged}/{killsToFinish}";
        }
        public string ToNetworkString(MatchState overrideStateForString)
        {
            return $"{playroomID}/{name}/{isPublic}/empty_password/{map}/{PlayersCurrAmount}/{maxPlayers}/{overrideStateForString}/{playersToStart}/{totalTimeToFinishInSeconds}/{totalTimeToFinishInSecUnchanged}/{killsToFinish}";
        }

        public string OnScoresChange(Player playerToIgnore)
        {
            if (this == null) return "none"; 
            string newScoresString = GeneratePlayersScoresString();
            if (newScoresString.Equals("none")) return newScoresString;
            SendMessageToAllPlayersInPlayroom($"{PLAYERS_SCORES_IN_PLAYROOM}|"+newScoresString, playerToIgnore);
            return newScoresString;
        }

        // players_scores|data @data@data
        // {fullFataOfPlayersInThatRoom} => db_id/nickname/kills/deaths@db_id/nickname/kills/deaths@db_id/nickname/kills/deaths
        public string GeneratePlayersScoresString()
        {
            try
            {
                if (playersInPlayroom == null) return "none";
                string result = "";
                for (int i = 0; i < playersInPlayroom.Count; i++)
                {
                    if (i < playersInPlayroom.Count - 1)
                    {
                        result += $"{playersInPlayroom[i].client.userData.db_id}/{playersInPlayroom[i].client.userData.nickname}/" +
                            $"{playersInPlayroom[i].stats_kills}/{playersInPlayroom[i].stats_deaths}@";
                    }
                    else
                    {
                        result += $"{playersInPlayroom[i].client.userData.db_id}/{playersInPlayroom[i].client.userData.nickname}/" +
                            $"{playersInPlayroom[i].stats_kills}/{playersInPlayroom[i].stats_deaths}";
                    }
                }
                return result;
            }
            catch (Exception e) { Console.WriteLine(e); }
            return "none";
            
        }

        //*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*\\
        #region Managing playroom state and other interactive events

        public bool IsThisNewPlayerWillStartTheMatch()
        {
            if (PlayersCurrAmount + 1 == playersToStart) return true;
            return false;
        }
        static Random random = new Random();
        public void ManageMatchStart(Player toIgnore, out Vector3 playerToIgnoreSpawnPos)
        {
            playerToIgnoreSpawnPos = Vector3.Zero;
            if (!matchState.Equals(MatchState.WaitingForPlayers)) 
            {
                // still need to assign newcoming player spawn position
                playerToIgnoreSpawnPos = GetRandomSpawnPointByMap_FarthestPos(map, this, toIgnore);
                return; 
            }
            // else -> below
            if (PlayersCurrAmount >= playersToStart)
            {
                matchState = MatchState.JustStarting;
                Vector3[] spawnPositions = GetRandomSpawnPointByMap_UnrepeatablePosition(map, playersInPlayroom.Count, out bool useRandomPositions);

                for (int i = 0; i < playersInPlayroom.Count; i++)
                {
                    Vector3 newPosition;
                    if (!useRandomPositions)
                        newPosition = spawnPositions[i];
                    else newPosition = spawnPositions[random.Next(0, spawnPositions.Length)];

                    if (playersInPlayroom[i] == toIgnore)
                    {
                        toIgnore.position = newPosition;
                        playerToIgnoreSpawnPos = newPosition;
                        //Console.WriteLine($"Found newcoming player. useRandomPositions:{useRandomPositions}, new position: {newPosition}");
                        continue;
                    }

                    playersInPlayroom[i].currentJumpsAmount = PlayroomManager.maxJumpsAmount;
                    Util_Server.SendMessageToClient($"{MATCH_STARTED_FORCE_OVERRIDE_POSITION_AND_JUMPS}|{playersInPlayroom[i].currentJumpsAmount}|" +
                        $"{newPosition.X}/{newPosition.Y}/{newPosition.Z}", playersInPlayroom[i].client.ch, MessageProtocol.TCP);
                }
                LaunchPlayroomTimer();
            }
            else
            {
                playerToIgnoreSpawnPos = GetRandomSpawnPointByMap_FarthestPos(map, this, toIgnore);
                return;
            }
        }

        void LaunchPlayroomTimer()
        {
            MatchTimer = new Task(ManageTimeLeft);
            MatchTimer.Start();
        }

        public Task MatchTimer;
        void ManageTimeLeft()
        {
            while (matchState.Equals(MatchState.InGame) || matchState.Equals(MatchState.JustStarting))
            {
                totalTimeToFinishInSeconds--;
                SendMessageToAllPlayersInPlayroom($"{MATCH_TIME_REMAINING}|{totalTimeToFinishInSeconds}", null, MessageProtocol.TCP);
                if (totalTimeToFinishInSeconds <= 0)
                {
                    // finish
                    FinishMatch(MatchFinishReason.FinishedByTime);
                }else if(matchState == MatchState.JustStarting)
                {
                    if (totalTimeToFinishInSecUnchanged - totalTimeToFinishInSeconds >= totalMatchStartTime)
                        matchState = MatchState.InGame;
                }
                Thread.Sleep(1000);
            }
        }

        public void CheckKillsForFinish()
        {
            //Console.WriteLine("________Check Kills For finish");
            foreach(Player a in playersInPlayroom)
            {
                if(a.stats_kills >= killsToFinish)
                {
                    //Console.WriteLine($"____a.stats_kills: {a.stats_kills}_killsToFinish: {killsToFinish}___Check Kills For finish");
                    FinishMatch(MatchFinishReason.FinishedByKills);
                    break;
                }
            }
        }
        async void FinishMatch(MatchFinishReason finishReason)
        {
            List<Player> winners = DefineWinnersOfTheMatch(finishReason, out int maxKills);
            matchState = MatchState.Finished;
            if (finishReason.Equals(MatchFinishReason.Discarded) || winners == null || winners.Count == 0)
            {
                Console.WriteLine($"[{DateTime.Now}][PLAYROOM_MESSAGE]: Finished match with id [{playroomID}], finish reason [{finishReason}], -> No winners");
                //SendMessageToAllPlayersInPlayroom($"{MATCH_FINISHED}|none|none|{MatchResult.Discarded}", null, MessageProtocol.TCP);
                SendMessageToAllPlayersInPlayroom(GenerateMatchResultsString("none", "none", MatchResult.Discarded.ToString()), null, MessageProtocol.TCP);
            }
            else
            {
                MatchResult matchResult;
                string winnerDbId;
                string winnerNickname;
                Player winner = null;
                if (winners.Count > 1)
                {
                    matchResult = MatchResult.Draw;
                    winnerDbId = "none";
                    winnerNickname = "none";
                    Console.WriteLine($"[{DateTime.Now}][PLAYROOM_MESSAGE]: Finished match with id [{playroomID}], match result [{matchResult}], max kills: [{maxKills}]");
                }
                else
                {
                    matchResult = MatchResult.PlayerWon;
                    winnerDbId = winners[0].client.userData.db_id.ToString();
                    winner = winners[0];
                    winnerNickname = winners[0].client.userData.nickname;
                    Console.WriteLine($"[{DateTime.Now}][PLAYROOM_MESSAGE]: Finished match with id [{playroomID}], match result [{matchResult}], winner [{winnerNickname}] max kills: [{maxKills}]");
                }
                SendMessageToAllPlayersInPlayroom(GenerateMatchResultsString(winnerDbId, winnerNickname, matchResult.ToString()), null, MessageProtocol.TCP);
                await RecordMatchResults(winner);
            }
            DelayedClosePlayroom = new Task(ClosePlayroomWithDelay);
            DelayedClosePlayroom.Start();
        }

        //dbId,nickname,kills,deaths,runes@
        string GenerateMatchResultsString(string winnerDbId, string winnerNickname, string matchResult)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{MATCH_FINISHED}|{winnerDbId}|{winnerNickname}|{matchResult}|");

            for (int i = 0; i < playersInPlayroom.Count; i++)
            {
                sb.Append($"{playersInPlayroom[i].client.userData.db_id},{playersInPlayroom[i].client.userData.nickname},{playersInPlayroom[i].stats_kills},{playersInPlayroom[i].stats_deaths},{playersInPlayroom[i].stats_runesPickedUp}");
                if (i != playersInPlayroom.Count - 1)
                    sb.Append("@");
            }

            return sb.ToString();
        }

        async Task RecordMatchResults(Player winner)
        {
            foreach(var a in playersInPlayroom)
            {
                UserData newUserData = new UserData(a.client.userData);

                newUserData.total_games++;
                if (a == winner)
                    newUserData.total_victories++;
                //newUserData.kills += a.stats_kills;
                //newUserData.deaths += a.stats_deaths;
                //newUserData.runes_picked_up += a.stats_runesPickedUp;

                UserData updated = await Client.UpdateUserData(a.client, newUserData);
                if(updated != null)
                    Console.WriteLine($"[{DateTime.Now}][PLAYROOM_MESSAGE]: Successfully recorded match results for player [{a.client.userData.db_id}][{a.client.userData.nickname}]");
                else Console.WriteLine($"[{DateTime.Now}][PLAYROOM_ERROR]: Couldn't record match results for player [{a.client.userData.db_id}][{a.client.userData.nickname}]");
            }
        }

        static int minKillsToCountAsVictory = 3;
        List<Player> DefineWinnersOfTheMatch(MatchFinishReason finishReason, out int maxKills)
        {
            maxKills = 0;
            if (finishReason.Equals(MatchFinishReason.Discarded)) return null;

            List<Player> winners = new List<Player>();

            int maxKillsInMatch = 0;
            foreach(Player a in playersInPlayroom)
                if (a.stats_kills > maxKillsInMatch) maxKillsInMatch = a.stats_kills;

            maxKills = maxKillsInMatch;
            if (maxKillsInMatch <= minKillsToCountAsVictory) return null;

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
            PlayroomManager.ClosePlayroom(this);
        }

        #endregion

        //  code|rune_effect_data@rune_effect_data
        // "rune_effects_info|player_db_id,runeType, runeType,runeType@player_db_id,runeType
        public const string RUNE_EFFECTS_INFO = "rune_effects_info";
        public string CurrentRuneEffectsToString(Player playerToIgnore = null)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                int i = 0;
                foreach (var player in playersInPlayroom)
                {
                    if (player == playerToIgnore) continue;
                    int j = 0;
                    if (i != 0) sb.Append("@");
                    sb.Append($"{player.client.userData.db_id},");
                    if (player.modifiersManager.activeRuneEffects.Count == 0)
                    {
                        sb.Append($"none");
                    }
                    else
                    {
                        foreach (var b in player.modifiersManager.activeRuneEffects)
                        {
                            if (j != 0) sb.Append(",");
                            sb.Append($"{b.assignedRune}");
                            j++;
                        }
                    }
                    i++;
                }
                return sb.ToString();
            }
            catch (Exception e) { Console.WriteLine(e); }
            return "none";
        }

        #region CustomCommandsFromAdminPlayer

        // code|RuneType|AmountEnum|SpawnPosEnum|notifyOthers
        public void AdminCommand_SpawnRunes(string message, Player invoker)
        {
            try
            {
                string[] substrings = message.Split("|");
                Enum.TryParse(substrings[1], out DataTypes.Rune rune);
                Enum.TryParse(substrings[2], out DataTypes.CustomRuneSpawn_Amount amount);
                Enum.TryParse(substrings[3], out DataTypes.CustomRuneSpawn_Position spawnPosition);
                Boolean.TryParse(substrings[4], out bool notifyOthers);

                runesManager.SpawnRunes_AdminCommand(invoker, rune, amount, spawnPosition, notifyOthers);
            }
            catch (Exception e) { Console.WriteLine(e); }
        }

        #endregion
    }
}
