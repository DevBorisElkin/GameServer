using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static GameServer.Util_Connection;

namespace GameServer
{
    public class Player
    {
        public ClientHandler ch;
        public string username;

        public Vector3 position;
        public Quaternion rotation;


        public Player(ClientHandler ch, string username, Vector3 spawnPosition)
        {
            this.ch = ch;
            this.username = username;
            position = spawnPosition;
            rotation = Quaternion.Identity;
        }

        public void SendPositionsOfOtherPlayers(string message)
        {
            Util_Server.SendMessageToClient(message, ch, MessageProtocol.UDP);
        }
    }
}
