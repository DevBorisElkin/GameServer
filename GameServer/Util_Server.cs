﻿using System;
using System.Net;
using System.Net.Sockets;
using static GameServer.Server;
using static GameServer.Util_Connection;
using static GameServer.NetworkingMessageAttributes;

namespace GameServer
{
    public static class Util_Server
    {
        #region Util
        public static bool AlreadyHasThisClient(Socket socket)
        {
            if (TryToGetClientWithIp(GetRemoteIp(socket)) == null) return false;
            return true;
        }

        public static int GetFirstFreeId()
        {
            ClientHandler util;
            for (int i = 1; i < 10000; i++)
            {
                if (!clients.TryGetValue(i, out util))
                {
                    return i;
                }
            }
            Console.WriteLine("[SERVER_ERROR]: Error getting first free id!");
            return -1;
        }
        public static ClientHandler TryToGetClientWithId(int id)
        {
            ClientHandler util;
            if (clients.TryGetValue(id, out util)) { return util; }
            else Console.WriteLine($"[Server]: Didn't find client with id {id}");
            return null;
        }
        public static ClientHandler TryToGetClientWithIp(string ip)
        {
            foreach (var a in clients.Values)
            {
                if (a.ip.Equals(ip)) return a;
            }
            //Console.WriteLine($"[Server]: Didn't find client with ip {ip}");
            return null;
        }

        public static void DisposeAllClients()
        {
            foreach (var a in Server.clients.Values)
            {
                a.ShutDownClient(0, false);
            }
            clients = null;
        }

        #region MESSAGING CAN BE PERFORMED ONLY HERE !
        // [SEND MESSAGE]
        public static void SendMessageToAllClients(string message, MessageProtocol mp = MessageProtocol.TCP, ClientHandler clientToIgnore = null)
        {
            message += END_OF_FILE;
            if (mp.Equals(MessageProtocol.TCP))
            {
                foreach (var a in clients.Values)
                {
                    if (clientToIgnore == a) continue;
                    a.SendMessageTcp(message);
                }
            }
            else
            {
                foreach (var a in clients.Values)
                {
                    if (clientToIgnore == a) continue;
                    UDP.SendMessageUdp(message, a);
                }
            }
        }
        public static void SendMessageToAllClientsInPlayroom(string message, MessageProtocol mp = MessageProtocol.TCP, ClientHandler clientToIgnore = null)
        {
            message += END_OF_FILE;
            if (mp.Equals(MessageProtocol.TCP))
            {
                foreach (var a in clients.Values)
                {
                    if (clientToIgnore == a) continue;
                    if (a.player == null) continue;
                    a.SendMessageTcp(message);
                }
            }
            else
            {
                foreach (var a in clients.Values)
                {
                    if (clientToIgnore == a) continue;
                    if (a.player == null) continue;
                    UDP.SendMessageUdp(message, a);
                }
            }
        }
        public static void SendMessageToClient(string message, string ip, MessageProtocol mp = MessageProtocol.TCP)
        {
            message += END_OF_FILE;
            ClientHandler clientHandler = TryToGetClientWithIp(ip);

            if (clientHandler == null) return;
            if (mp.Equals(MessageProtocol.TCP))
            {
                clientHandler.SendMessageTcp(message);
            }
            else
            {
                UDP.SendMessageUdp(message, clientHandler);
            }
            
        }
        public static void SendMessageToClient(string message, int id)
        {
            message += END_OF_FILE;
            ClientHandler clientHandler = TryToGetClientWithId(id);
            if (clientHandler != null) clientHandler.SendMessageTcp(message);
        }
        public static void SendMessageToClient(string message, ClientHandler client, MessageProtocol mp = MessageProtocol.TCP)
        {
            message += END_OF_FILE;
            if (mp.Equals(MessageProtocol.TCP))
                client.SendMessageTcp(message);
            else
                UDP.SendMessageUdp(message, client);
        }
        #endregion MESSAGIND END ------
        public static IPAddress GetIpOfServer()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            return ipAddress;
        }
        #endregion

        #region Delegates
        public delegate void OnServerStartedDelegate();
        public static event OnServerStartedDelegate OnServerStartedEvent;

        public delegate void OnServerShutDownDelegate();
        public static event OnServerShutDownDelegate OnServerShutDownEvent;

        public delegate void OnClientConnectedDelegate(ClientHandler client);
        public static event OnClientConnectedDelegate OnClientConnectedEvent;

        public delegate void OnClientDisconnectedDelegate(ClientHandler client, string error);
        public static event OnClientDisconnectedDelegate OnClientDisconnectedEvent;

        public delegate void OnMessageReceivedDelegate(string message, ClientHandler ch, MessageProtocol mp);
        public static event OnMessageReceivedDelegate OnMessageReceivedEvent;

        public static void OnServerStarted() { OnServerStartedEvent?.Invoke(); }
        public static void OnServerShutDown() { OnServerShutDownEvent?.Invoke(); }
        public static void OnClientConnected(ClientHandler client) { OnClientConnectedEvent?.Invoke(client); }
        public static void OnClientDisconnected(ClientHandler client, string error) { OnClientDisconnectedEvent?.Invoke(client, error); }
        public static void OnMessageReceived(string message, ClientHandler ch, MessageProtocol mp) { OnMessageReceivedEvent?.Invoke(message, ch, mp); }
        #endregion

        #region Debug

        public static void CustomDebug_ShowClients()
        {
            Console.WriteLine($"Clients amount: {Server.clients.Count}");
            Console.WriteLine();
            int i = 1;
            foreach(var a in Server.clients.Values)
            {
                Console.WriteLine($"#{i} [{a.id}][{a.ip}]");
                i++;
            }
            Console.WriteLine();
        }

        public static void CustomDebug_ShowStoredIPs()
        {
            Console.WriteLine($"IEndPoints amount: {Util_UDP.endpoints.Count}");
            int i = 1;
            foreach(UnassignedIPEndPoint a in Util_UDP.endpoints)
            {
                Console.WriteLine($"#{i} {a.endPoint.ToString()}");
            }
            Console.WriteLine();
        }

        #endregion
    }
}