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
            OnMessageReceivedEvent += MessageReceived;
        }

        void ReadConsole()
        {
            string consoleString = Console.ReadLine();

            if (consoleString != "")
            {
                if (consoleString.StartsWith("tcp "))
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
        
        // TODO! !!!!!!!!!!!!!!!!!!!!!!
        void MessageReceived(string message, ClientHandler ch, MessageProtocol mp) 
        {
            //Console.WriteLine($"[CLIENT_MESSAGE][{mp}][{id}][{ip}]: {message}"); 

            // normally here should be some logic, checking, if specific playroom has space for new players to join
            if (message.StartsWith(ENTER_PLAY_ROOM))
            {
                string[] substrings = message.Split("|");

                ch.ConnectPlayerToPlayroom(Int32.Parse(substrings[1]), substrings[2]);

                Console.WriteLine($"[{ch.id}][{ch.ip}]Client requested to connect to playroom and was accepted");
            }else if (message.StartsWith(CLIENT_SHARES_PLAYROOM_POSITION))
            {
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

                ch.StorePlayerPositionAndRotationOnServer(position, rotation);
            }else if (message.StartsWith(CLIENT_DISCONNECTED_FROM_THE_PLAYROOM))
            {
                Console.WriteLine($"[{ch.id}][{ch.ip}][{message}] Client disconnected from playroom");
                string[] substrings = message.Split("|");
                ch.DisconnectPlayerFromPlayroom(int.Parse(substrings[1]), substrings[2]);
            }
            else
            {
                Console.WriteLine($"[CLIENT_MESSAGE][{mp}][{ch.id}][{ch.ip}]: {message}"); 
            }



        }
    }
}