using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static GameServer.Server;
using static GameServer.NetworkingMessageAttributes;

namespace GameServer
{
    public static class ClientHandlerExtensions
    {
        public static void ConnectPlayerToPlayroom(this ClientHandler client, int playroomNumber, string nickname)
        {
            // normally here should be some logic, checking, if specific playroom has space for new players to join

            client.player = new Player(client.id, nickname, Vector3.Zero);

            // tell the client that he is accepted
            client.SendMessageTcp($"{CONFIRM_ENTER_PLAY_ROOM}|{playroomNumber}");

            // tell all other clients who are in Playroom that one client connected to it
            client.server.SendMessageToAllClientsInPlayroom($"{CLIENT_CONNECTED_TO_THE_PLAYROOM}|{playroomNumber}|" +
                $"{client.player.position.X},{client.player.position.Y},{client.player.position.Z}|{nickname}|{client.ip}", MessageProtocol.TCP, client);

            client.server.TurnOn_Playroom();
        }
        public static void DisconnectPlayerFromPlayroom(this ClientHandler client, int playroomNumber, string nickname, string ip)
        {
            client.player = null;

            // tell all other clients who are in Playroom that one client connected to it
            client.server.SendMessageToAllClientsInPlayroom($"{CLIENT_DISCONNECTED_FROM_THE_PLAYROOM}|{playroomNumber}|{nickname}|{ip}", MessageProtocol.TCP, client);

            client.server.TurnOff_Playroom();
        }
        public static void StorePlayerPositionAndRotationOnServer(this ClientHandler client, Vector3 _position, Quaternion _rotation)
        {
            if(client.player != null)
            {
                client.player.position = _position;
                client.player.rotation = _rotation;
            }
        }

    }
}
