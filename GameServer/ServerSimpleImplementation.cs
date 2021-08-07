using System;
using static GameServer.NetworkingMessageAttributes;
using static GameServer.Util_Connection;
using static GameServer.Util_Server;
using System.Numerics;
using System.Globalization;

namespace GameServer
{
    // SIMPLE AND QUICK IMPLEMENTATION
    class ServerSimpleImplementation
    {
        int portTcp = 8384;
        int portUdp = 8385;
        public ServerSimpleImplementation()
        {
            SubscribeToEvents();
            
            Server.StartServer(portTcp, portUdp);
            
            while (true) ReadConsole();
        }
        void SubscribeToEvents()
        {
            OnServerStartedEvent += ServerStarted;
            OnServerShutDownEvent += ServerShutDown;
            OnClientConnectedEvent += ClientConnected;
            OnClientDisconnectedEvent += ClientDisconnected;
            OnMessageReceivedEvent += Connection_MessageReceived;
        }

        void ReadConsole()
        {
            string consoleString = Console.ReadLine();

            if (consoleString != "")
            {
                if (consoleString.StartsWith('-'))
                {
                    if (consoleString.Equals("-clients"))
                    {
                        CustomDebug_ShowClients();
                    }
                    else if (consoleString.Equals("-keys"))
                    {
                        CustomDebug_ShowStoredIPs();
                    }
                }else if (consoleString.StartsWith("tcp "))
                {
                    consoleString = consoleString.Replace("tcp ", "");
                    SendMessageToAllClients(consoleString);
                }
                else if (consoleString.StartsWith("udp "))
                {
                    consoleString = consoleString.Replace("udp ", "");
                    SendMessageToAllClients(consoleString, MessageProtocol.UDP);
                }
                else
                {
                    SendMessageToAllClients(consoleString);
                }
            }
        }

        void ServerStarted() { Console.WriteLine($"[SERVER_LAUNCHED][{Server.ip}]"); }
        void ServerShutDown() { Console.WriteLine($"[SERVER_SHUTDOWN][{Server.ip}]"); }
        void ClientConnected(ClientHandler clientHandler) { Console.WriteLine($"[CLIENT_CONNECTED][{clientHandler.id}][{clientHandler.ip}]"); }
        void ClientDisconnected(ClientHandler clientHandler, string error) { Console.WriteLine($"[CLIENT_DISCONNECTED][{clientHandler.id}][{clientHandler.ip}]: {error}"); }
    }
}