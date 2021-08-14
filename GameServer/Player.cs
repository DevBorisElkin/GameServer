using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using static GameServer.Util_Connection;
using static GameServer.NetworkingMessageAttributes;

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


        public Player(ClientHandler ch, string username, Vector3 spawnPosition)
        {
            this.ch = ch;
            this.username = username;
            position = spawnPosition;
            rotation = Quaternion.Identity;
            lastShotTime = DateTime.Now;
            lastJumpTime = DateTime.Now;

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
            var msSinceLastJumpWasMade = (DateTime.Now - lastJumpTime).TotalMilliseconds;

            if (msSinceLastJumpWasMade <= TimeSpan.FromSeconds(PlayroomManager.jumpCooldownTime).TotalMilliseconds) return; // basically he needs to wait for reload

            lastJumpTime = DateTime.Now;
            Util_Server.SendMessageToClient($"{JUMP_RESULT}", ch, MessageProtocol.TCP);
        }
    }
}
