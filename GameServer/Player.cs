using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using static GameServer.Util_Server;
using static GeneralUsage.NetworkingMessageAttributes;
using static GameServer.PlayroomManager;

namespace GameServer
{
    public class Player
    {
        public ClientHandler ch;
        public string username;

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


        public Player(ClientHandler ch, string username, Vector3 spawnPosition)
        {
            this.ch = ch;
            this.username = username;
            position = spawnPosition;
            rotation = Quaternion.Identity;
            lastShotTime = DateTime.Now;
            lastJumpTime = DateTime.Now;

            currentJumpsAmount = maxJumpsAmount;
            isRecoveringJump = false;

            stats_kills = 0;
            stats_deaths = 0;
        }

        public void CheckAndMakeShot(string message)
        {
            var msSinceLastShotWasMade = (DateTime.Now - lastShotTime).TotalMilliseconds;
            
            if (msSinceLastShotWasMade <= TimeSpan.FromSeconds(PlayroomManager.reloadTime).TotalMilliseconds) return; // basically he needs to wait for reload

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

            // code|posOfShootingPoint|rotationAtRequestTime|ipOfShootingPlayer
            // "shot_result|123/45/87|543/34/1|198.0.0.126";
            string msg = $"{SHOT_RESULT}|{position.X}/{position.Y}/{position.Z}|{rotation.X}/{rotation.Y}/{rotation.Z}|{ch.ip}";
            playroom.SendMessageToAllPlayersInPlayroom(msg, null, MessageProtocol.TCP);
        }
        public void CheckAndMakeJump()
        {
            if(currentJumpsAmount > 0)
            {
                currentJumpsAmount--;

                if (!isRecoveringJump)
                {
                    startedRecoveringJump = DateTime.Now;
                    isRecoveringJump = true;
                }

                //Console.WriteLine("Trying to send to client: "+ $"{JUMP_RESULT}|{currentJumpsAmount}");
                Util_Server.SendMessageToClient($"{JUMP_RESULT}|{currentJumpsAmount}", ch, MessageProtocol.TCP);
            }
        }

        public void CheckAndAddJumps()
        {
            if (currentJumpsAmount >= maxJumpsAmount) return;
            if (!isRecoveringJump) return;


            var msSinceStartedRecoveringJump = (DateTime.Now - startedRecoveringJump).TotalMilliseconds;

            if (msSinceStartedRecoveringJump >= TimeSpan.FromSeconds(PlayroomManager.jumpCooldownTime).TotalMilliseconds)
            {
                if(currentJumpsAmount + 1 >= maxJumpsAmount)
                {
                    currentJumpsAmount += 1;
                    isRecoveringJump = false;
                }
                else
                {
                    currentJumpsAmount += 1;
                    startedRecoveringJump = DateTime.Now;
                }
                //Console.WriteLine("Trying to send to client: " + $"{JUMP_AMOUNT}|{currentJumpsAmount}");
                Util_Server.SendMessageToClient($"{JUMP_AMOUNT}|{currentJumpsAmount}|false", ch, MessageProtocol.TCP);
            }
        }
        // "player_died|killer_ip|reasonOfDeath
        public void PlayerDied(string message)
        {
            string[] substrings = message.Split("|");
            // TODO CHANGE SCORE

            currentJumpsAmount = maxJumpsAmount;
            isRecoveringJump = false;
            Util_Server.SendMessageToClient($"{JUMP_AMOUNT}|{currentJumpsAmount}|true", ch, MessageProtocol.TCP);
        }
    }
}
