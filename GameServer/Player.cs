using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using static ServerCore.Util_Server;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.ClientHandler;
using static ServerCore.PlayroomManager;
using static ServerCore.PlayroomManager_MapData;
using static ServerCore.ModifiersManager;
using static ServerCore.DataTypes;
using System.Threading.Tasks;
using System.Threading;

namespace ServerCore
{
    public class Player
    {
        public ServerCore.Client client;

        public Vector3 position;
        public Quaternion rotation;
        public DateTime lastShotTime;
        public DateTime lastJumpTime;

        public Playroom playroom;

        public int stats_kills;
        public int stats_deaths;

        public int currentJumpsAmount;
        bool isRecoveringJump;
        DateTime startedRecoveringJump;

        public bool isAlive;

        public ModifiersManager modifiersManager;


        public Player(Client client, Vector3 spawnPosition)
        {
            this.client = client;
            position = spawnPosition;
            rotation = Quaternion.Identity;
            lastShotTime = DateTime.Now;
            lastJumpTime = DateTime.Now;

            currentJumpsAmount = maxJumpsAmount;
            isRecoveringJump = false;

            stats_kills = 0;
            stats_deaths = 0;

            isAlive = true;

            modifiersManager = new ModifiersManager(this);
        }

        public void CheckAndMakeShot(string message)
        {
            if (!isAlive) return;
            var msSinceLastShotWasMade = (DateTime.Now - lastShotTime).TotalMilliseconds;
            
            if (msSinceLastShotWasMade <= TimeSpan.FromSeconds(PlayroomManager.reloadTime).TotalMilliseconds * modifiersManager.GetReloadTimeMultiplier()) return; // basically he needs to wait for reload

            lastShotTime = DateTime.Now;

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

            // message to players - shows shot data
            // code|posOfShootingPoint|rotationAtRequestTime|dbIdOfShootingPlayer|activeRuneModifiers
            // activeRuneModifiers: rune@rune@rune  or "none"
            // "shot_result|123/45/87|543/34/1|13|Black/LightBlue/Red";
            
            string msg = $"{SHOT_RESULT}|{position.X}/{position.Y}/{position.Z}|{rotation.X}/{rotation.Y}/{rotation.Z}" +
                $"|{client.userData.db_id}|{modifiersManager.ShotModifiersIntoString()}";
            playroom.SendMessageToAllPlayersInPlayroom(msg, null, MessageProtocol.TCP);
        }
        public void CheckAndMakeJump()
        {
            if (!isAlive) return;
            if (currentJumpsAmount > 0)
            {
                currentJumpsAmount--;

                if (!isRecoveringJump)
                {
                    startedRecoveringJump = DateTime.Now;
                    isRecoveringJump = true;
                }

                //Console.WriteLine("Trying to send to client: "+ $"{JUMP_RESULT}|{currentJumpsAmount}");
                Util_Server.SendMessageToClient($"{JUMP_RESULT}|{currentJumpsAmount}", client.ch, MessageProtocol.TCP);
            }
        }

        public void CheckAndAddJumps(int i = 1, bool forceAdd = false)
        {
            if (currentJumpsAmount >= maxJumpsAmount) return;
            if (!isRecoveringJump) return;

            var msSinceStartedRecoveringJump = (DateTime.Now - startedRecoveringJump).TotalMilliseconds;

            if (forceAdd || msSinceStartedRecoveringJump >= TimeSpan.FromSeconds(PlayroomManager.jumpCooldownTime).TotalMilliseconds)
            {
                if(currentJumpsAmount + i >= maxJumpsAmount)
                {
                    currentJumpsAmount = maxJumpsAmount;
                    isRecoveringJump = false;
                }
                else
                {
                    currentJumpsAmount += i;
                    startedRecoveringJump = DateTime.Now;
                }
                //Console.WriteLine("Trying to send to client: " + $"{JUMP_AMOUNT}|{currentJumpsAmount}");
                Util_Server.SendMessageToClient($"{JUMP_AMOUNT}|{currentJumpsAmount}|false", client.ch, MessageProtocol.TCP);
            }
        }
        // "player_died|killer_dbId|reasonOfDeath
        public void PlayerDied(string message)
        {
            isAlive = false;

            modifiersManager.ResetModifiers();

            ChangeScoresOnPlayerDied(message);

            currentJumpsAmount = maxJumpsAmount;
            isRecoveringJump = false;
            //Util_Server.SendMessageToClient($"{JUMP_AMOUNT}|{currentJumpsAmount}|true", ch, MessageProtocol.TCP);

            revivePlayer = null;
            revivePlayer = new Task(RevivePlayer);
            revivePlayer.Start();
        }

        //  code|player_db_id|runeType|runeUniqueId
        // "rune_try_to_pick_up|12|Black|5"
        //public const string RUNE_TRY_TO_PICK_UP = "rune_try_to_pick_up";
        public void PlayerTriesToPickUpRune(string message)
        {
            if (!isAlive) return;
            try
            {
                string[] substrings = message.Split("|");
                int playerDbId = Int32.Parse(substrings[1]);
                if (playerDbId != client.userData.db_id) Console.WriteLine($"[{DateTime.Now}][PLAYER_ERROR] OnPickUpRune_message -> " +
                    $"Player db.id. not equal what's in the message [{client.userData.db_id}][{playerDbId}]");
                Enum.TryParse(substrings[2], out Rune runeType);
                int runeId = Int32.Parse(substrings[3]);

                playroom.runesManager.PlayerTriesToPickUpRune(runeId, this);
            }
            catch (Exception e) { Console.WriteLine(e); }
        }

        Task revivePlayer;
        void RevivePlayer()
        {
            Thread.Sleep(3000);

            Vector3 spawnPos = GetRandomSpawnPointByMap_FarthestPos(playroom.map, playroom, this);
            isAlive = true;
            // SEND MESSAGE TO OTHER CLIENTS THAT PLAYER DIED AND SPAWN PARTICLES
            Util_Server.SendMessageToAllClients($"{SPAWN_DEATH_PARTICLES}|{position.X}/{position.Y}/{position.Z}|{rotation.X}/{rotation.Y}/{rotation.Z}", MessageProtocol.TCP, null);
            Util_Server.SendMessageToClient($"{PLAYER_REVIVED}|{spawnPos.X}/{spawnPos.Y}/{spawnPos.Z}|{currentJumpsAmount}", client.ch, MessageProtocol.TCP);
        }

        void ChangeScoresOnPlayerDied(string message)
        {
            if (playroom.matchState != MatchState.InGame) return;
            stats_deaths++;

            string[] substrings = message.Split("|");
            int killerDbId = Int32.Parse(substrings[1]);
            string deathDetails = substrings[2];

            Player killer = null;

            // find killer player if exists and assign points
            if (!killerDbId.Equals(-1) && !killerDbId.Equals(""))
            {
                foreach(Player pl in playroom.playersInPlayroom)
                {
                    if (pl.client.userData.db_id == killerDbId) 
                    {
                        killer = pl;
                        break;
                    }  
                }
                if (killer != null)
                {
                    killer.stats_kills++;
                    playroom.CheckKillsForFinish();
                }
            }
            playroom.OnScoresChange(null);
            // player_was_killed_message|playerDeadNickname/playerDeadIP|playerKillerNickname/playerKilledIP|deathDetails

            if(killer != null)
                playroom.SendMessageToAllPlayersInPlayroom($"{PLAYER_WAS_KILLED_MESSAGE}|{client.userData.nickname}/{client.userData.db_id}|{killer.client.userData.nickname}/{killer.client.userData.nickname}|{deathDetails}", null, MessageProtocol.TCP);
            else
                playroom.SendMessageToAllPlayersInPlayroom($"{PLAYER_WAS_KILLED_MESSAGE}|{client.userData.nickname}/{client.userData.db_id}|none/none|{deathDetails}", null, MessageProtocol.TCP);

        }
    }
}
