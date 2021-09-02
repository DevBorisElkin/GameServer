using System;
using static ServerCore.Util_Server;
using System.Numerics;
using System.Globalization;
using ServerCore;
using DatabaseAccess;
using System.Collections.Generic;
using static ServerCore.NetworkingMessageAttributes;
using static ServerCore.Server;
using static ServerCore.PlayroomManager_MapData;
using static ServerCore.Client;

namespace EntryPoint
{
    class Program
    {
        static void Main(string[] args)
        {
            new ServerSimpleImplementation();
        }
        // SIMPLE AND QUICK IMPLEMENTATION
        public class ServerSimpleImplementation
        {
            int portTcp = 8384;
            int portUdp = 8385;
            public ServerSimpleImplementation()
            {
                DatabaseBridge.InitDatabase();
                PlayroomManager.InitPlayroomManager();
                SubscribeToEvents();
                connected_clients = new List<Client>();
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
                    }
                    else if (consoleString.StartsWith("tcp "))
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

            void ServerStarted() { Console.WriteLine($"[{DateTime.Now}][SERVER_LAUNCHED][{Server.ip}]"); }
            void ServerShutDown() { Console.WriteLine($"[{DateTime.Now}][SERVER_SHUTDOWN][{Server.ip}]"); }
            void ClientConnected(ClientHandler clientHandler) 
            { 
                Console.WriteLine($"[{DateTime.Now}][CLIENT_CONNECTED][{clientHandler.id}][{clientHandler.ip}]");
                connected_clients.Add(new Client(clientHandler));

            }
            void ClientDisconnected(ClientHandler clientHandler, string error) 
            {
                Console.WriteLine($"[{DateTime.Now}][CLIENT_DISCONNECTED][{clientHandler.id}][{clientHandler.ip}]: {error}");
                RemoveClient(clientHandler);
            }

            #region Parce Messages From Clients

            public async static void Connection_MessageReceived(string msg, ClientHandler ch, MessageProtocol mp)
            {
                Client assignedClient = GetClientByClientHandler(ch);
                if (assignedClient == null) { Console.WriteLine($"[{DateTime.Now}]Error, ch is not assigned to client"); return; }

                string[] parcedMessage = msg.Split(END_OF_FILE, StringSplitOptions.RemoveEmptyEntries);

                foreach (string message in parcedMessage)
                {
                    try
                    {
                        if (message.Contains(CHECK_CONNECTED)) continue;

                        // not showing CHECK_CONNECTED and SHARES_PLAYROOM because it spams in console
                        if (!message.Contains(CLIENT_SHARES_PLAYROOM_POSITION) && !message.Contains(SHOT_REQUEST) && !message.Contains(JUMP_REQUEST))
                        {
                            Console.WriteLine($"[{DateTime.Now}][CLIENT_MESSAGE][{mp}][{ch.id}][{ch.ip}]: {message} | {DateTime.Now}");
                        }

                        // _*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*_*

                        if (assignedClient.clientAccessLevel.Equals(ClientAccessLevel.LowestLevel))
                        {
                            // accept only requests for authentication and registration
                            if (message.Contains(LOG_IN))
                            {
                                // here should do some magic with database
                                string[] substrings = message.Split("|");
                                await DatabaseBridge.TryToAuthenticateAsync(substrings[1], substrings[2], assignedClient);

                            }
                            else if (message.Contains(REGISTER))
                            {
                                string[] substrings = message.Split("|");
                                DatabaseBridge.TryToRegisterAsync(substrings[1], substrings[2], substrings[3], assignedClient);
                            }
                            else
                            {
                                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: 1) It appears that client {ch.ip} " +
                                    $"asks operation that he has no rights for, his request: {message}");
                            }

                        }
                        else if (assignedClient.clientAccessLevel.Equals(ClientAccessLevel.Authenticated))
                        {
                            // accept all other requests
                            if (DoesMessageRelatedToPlayroomManager(message))
                            {
                                ParceMessage_Playroom(message, assignedClient);
                            }
                            else
                            {
                                Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: 2) It appears that client {ch.ip} " +
                                    $"asks operation that he has no rights for, his request: {message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{DateTime.Now}]{e.Message} ||| {e.StackTrace} ||| message was:{message}");
                    }
                }
            }

            public static void ParceMessage_Playroom(string message, Client client)
            {
                try
                {
                    if (message.StartsWith(PLAYROOMS_DATA_REQUEST))
                    {
                        PlayroomManager.RequestFromClient_GetPlayroomsData(client);
                    }
                    else if (message.StartsWith(CREATE_PLAY_ROOM))
                    {
                        string[] substrings = message.Split("|");

                        bool.TryParse(substrings[2], out bool isPublic);
                        Enum.TryParse(substrings[4], out Map map);

                        PlayroomManager.RequestFromClient_CreatePlayroom(client, substrings[1], isPublic, substrings[3], map, 
                         Int32.Parse(substrings[5]), Int32.Parse(substrings[6]), Int32.Parse(substrings[7]), Int32.Parse(substrings[8]));
                    }
                    else if (message.StartsWith(ENTER_PLAY_ROOM))
                    // normally here should be some logic, checking, if specific playroom has space for new players to join
                    {
                        string[] substrings = message.Split("|");

                        Console.WriteLine($"[{DateTime.Now}][{client.ch.id}][{client.ch.ip}]Client requested to connect to playroom");
                        if (substrings.Length == 2)
                            PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), client);
                        else if (substrings.Length == 3)
                            PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), client, substrings[2]);
                    }
                    else if (message.StartsWith(CLIENT_SHARES_PLAYROOM_POSITION))
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

                        PlayroomManager.RequestFromClient_StorePlayerPositionAndRotation(client, position, rotation);
                    }
                    else if (message.StartsWith(CLIENT_DISCONNECTED_FROM_THE_PLAYROOM))
                    {
                        Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]:Client [{client.ch.id}][{client.ch.ip}] disconnected from playroom");
                        string[] substrings = message.Split("|");
                        PlayroomManager.RequestFromClient_DisconnectFromPlayroom(int.Parse(substrings[1]), client);
                    }
                    else if (message.StartsWith(SHOT_REQUEST))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.CheckAndMakeShot(message);
                        }
                    }
                    else if (message.StartsWith(JUMP_REQUEST))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.CheckAndMakeJump();
                        }
                    }
                    else if (message.StartsWith(PLAYER_DIED))
                    {
                        if (client.player != null && client.player.playroom != null)
                        {
                            client.player.PlayerDied(message);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            #endregion
        }
    }
}
