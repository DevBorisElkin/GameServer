using System;
using System.Net;
using System.Net.Sockets;
using static GameServer.Server;
using static GameServer.Util_Connection;
using static GameServer.NetworkingMessageAttributes;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;

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
            else Console.WriteLine($"[SERVER_MESSAGE]: Didn't find client with id {id}");
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

        public static IPAddress GetIpOfServer()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            return ipAddress;
        }
        #endregion

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

        #region Parce Messages From Clients

        public async static void Connection_MessageReceived(string msg, ClientHandler ch, MessageProtocol mp)
        {
            string[] parcedMessage = msg.Split(END_OF_FILE, StringSplitOptions.RemoveEmptyEntries);

            foreach (string message in parcedMessage)
            {
                try
                {
                    if (message.Contains(CHECK_CONNECTED)) continue;

                    // not showing CHECK_CONNECTED and SHARES_PLAYROOM because it spams in console
                    if (!message.Contains(CLIENT_SHARES_PLAYROOM_POSITION) && !message.Contains(SHOT_REQUEST) && !message.Contains(JUMP_REQUEST))
                    {   
                        Console.WriteLine($"[CLIENT_MESSAGE][{mp}][{ch.id}][{ch.ip}]: {message} | {DateTime.Now}");
                    }
                    if (message.Contains(CHECK_CONNECTED)) continue;

                    if (ch.clientAccessLevel.Equals(ClientAccessLevel.LowestLevel))
                    {
                        // accept only requests for authentication and registration

                        if (message.Contains(LOG_IN))
                        {
                            // here should do some magic with database
                            string[] substrings = message.Split("|");
                            await DatabaseBridge.TryToAuthenticateAsync(substrings[1], substrings[2], ch);

                        }else if (message.Contains(REGISTER))
                        {
                            string[] substrings = message.Split("|");
                            DatabaseBridge.TryToRegisterAsync(substrings[1], substrings[2], substrings[3], ch);
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER_MESSAGE]: It appears that client {ch.ip} " +
                                $"asks operation that he has no rights for, his request: {message}");
                        }

                    }
                    else if (ch.clientAccessLevel.Equals(ClientAccessLevel.Authenticated))
                    {
                        // accept all other requests
                        if (DoesMessageRelatedToPlayroomManager(message))
                        {
                            Util_PlayroomManager.ParceMessage_Playroom(message, ch);
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} ||| {e.StackTrace} ||| message was:{message}");
                }
            }
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
