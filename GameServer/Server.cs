﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static GameServer.Util_Connection;

namespace GameServer
{
    public static class Server
    {
        public static string ip;
        private static int portTcp;

        public static Dictionary<int, ClientHandler> clients;

        private static bool serverActive;
        private static Socket handler;
        private static Socket listenSocket;

        // [START SERVER]
        public static void StartServer(int _port, int _portUdp)
        {
            UDP.StartUdpServer(_portUdp);
            PlayroomManager.InitPlayroom();

            portTcp = _port;
            ip = Util_Server.GetIpOfServer().ToString();

            clients = new Dictionary<int, ClientHandler>();

            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, portTcp);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.Bind(ipEndPoint);
            listenSocket.Listen(5);
            serverActive = true;

            Util_Server.OnServerStarted();

            Task listenToConnectionsTask = new Task(ListenToNewConnections);
            listenToConnectionsTask.Start();

        }
        // [LISTEN TO CONNECTIONS]
        static void ListenToNewConnections()
        {
            try
            {
                while (serverActive)
                {
                    handler = listenSocket.Accept();
                    if (!Util_Server.AlreadyHasThisClient(handler))
                    {
                        int clientId = Util_Server.GetFirstFreeId();
                        ClientHandler client = new ClientHandler(handler, clientId);
                        AddClient(client, clientId);
                    }
                    else
                    {
                        Console.WriteLine($"[SERVER_MESSAGE] reject repetetive connection from {GetRemoteIp(handler)}");
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "\n Error 1");
            }
            finally
            {
                ShutDownServer();
            }
        }
        
        // [SHUT DOWN SERVER]
        public static void ShutDownServer()
        {
            serverActive = false;
            if (handler != null)
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            Util_Server.DisposeAllClients();
            Util_Server.OnServerShutDown();
        }
        // [ADD CLIENT]
        static void AddClient(ClientHandler client, int id)
        {
            Util_Server.OnClientConnected(client);
            clients[id] = client;
        }
        // [REMOVE CLIENT]
        public static void DisconnectClient(ClientHandler client)
        {
            client.ShutDownClient();
        }
    }
}