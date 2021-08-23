using System;
using System.Net;
using System.Net.Sockets;
using static GameServer.Server;
using static GeneralUsage.NetworkingMessageAttributes;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using static GeneralUsage.PlayroomManager_MapData;

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

        #region Util Connection
        public enum MessageProtocol { TCP, UDP }

        // Check if client connected
        public static bool SocketSimpleConnected(this ClientHandler ch)
        {
            return !((ch.handler.Poll(1000, SelectMode.SelectRead) && (ch.handler.Available == 0)) || !ch.handler.Connected);
        }
        public static bool SocketSimpleConnected(Socket tcpHandler)
        {
            return !((tcpHandler.Poll(1000, SelectMode.SelectRead) && (tcpHandler.Available == 0)) || !tcpHandler.Connected);
        }

        // Gets IP
        public static string GetRemoteIp(this ClientHandler ch)
        {
            string rawRemoteIP = ch.handler.RemoteEndPoint.ToString();
            int dotsIndex = rawRemoteIP.LastIndexOf(":");
            string remoteIP = rawRemoteIP.Substring(0, dotsIndex);
            return remoteIP;
        }
        public static string GetRemoteIp(EndPoint ep)
        {
            string rawRemoteIP = ep.ToString();
            int dotsIndex = rawRemoteIP.LastIndexOf(":");
            string remoteIP = rawRemoteIP.Substring(0, dotsIndex);
            return remoteIP;
        }
        public static string GetRemoteIp(Socket tcpHandler)
        {
            string rawRemoteIP = tcpHandler.RemoteEndPoint.ToString();
            int dotsIndex = rawRemoteIP.LastIndexOf(":");
            string remoteIP = rawRemoteIP.Substring(0, dotsIndex);
            return remoteIP;
        }
        // Get IP and Port
        public static string GetRemoteIpAndPort(this ClientHandler ch)
        {
            return ch.handler.RemoteEndPoint.ToString();
        }
        public static string GetRemoteIpAndPort(Socket tcpHandler)
        {
            return tcpHandler.RemoteEndPoint.ToString();
        }
        #endregion

        #region Util UDP -> stack UDP IPEndPoints and assign them when cient TCP connects
        public class UnassignedIPEndPoint
        {
            public IPEndPoint endPoint;
            public string ip;

            public UnassignedIPEndPoint(IPEndPoint _endPoint, string _ip)
            {
                endPoint = _endPoint;
                ip = _ip;
            }
        }

        public static List<UnassignedIPEndPoint> udpEndpoints = new List<UnassignedIPEndPoint>();

        public static void TryToStoreEndPoint(IPEndPoint endPoint, string ip)
        {
            // check if there's end point for that ip
            if (udpEndpoints.Count > 0)
            {
                foreach (var a in udpEndpoints)
                {
                    if (a.ip.Equals(ip))
                    {
                        return;
                    }
                }
            }
            Console.WriteLine($"[SERVER]: Stored IPEndPoint for client {ip}");
            udpEndpoints.Add(new UnassignedIPEndPoint(endPoint, ip));
        }

        public static IPEndPoint TryToRetrieveEndPoint(string ip)
        {
            IPEndPoint result = null;
            UnassignedIPEndPoint unassigned = null;


            foreach (var a in udpEndpoints)
            {
                if (a.ip.Equals(ip))
                {
                    unassigned = a;
                    break;
                }
            }

            if (unassigned != null)
            {
                result = unassigned.endPoint;
                udpEndpoints.Remove(unassigned);
            }
            return result;
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
                            ParceMessage_Playroom(message, ch);
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message} ||| {e.StackTrace} ||| message was:{message}");
                }
            }
        }

        public static void ParceMessage_Playroom(string message, ClientHandler ch)
        {
            try
            {
                if (message.StartsWith(PLAYROOMS_DATA_REQUEST))
                {
                    PlayroomManager.RequestFromClient_GetPlayroomsData(ch);
                }
                //          0           1          2         3     4      5
                // "create_playroom|nameOfRoom|is_public|password|map|maxPlayers";
                else if (message.StartsWith(CREATE_PLAY_ROOM))
                {
                    string[] substrings = message.Split("|");

                    bool.TryParse(substrings[2], out bool isPublic);
                    Enum.TryParse(substrings[4], out Map map);

                    PlayroomManager.RequestFromClient_CreatePlayroom(ch, substrings[1], isPublic,
                        substrings[3], map, Int32.Parse(substrings[5]));
                }
                // WILL REMAKE RESPONSE 'CONFIRM_ENTER_PLAYROOM' and there will be response: okay, or error
                // "enter_playroom|3251|the_greatest_password_ever";
                else if (message.StartsWith(ENTER_PLAY_ROOM))
                // normally here should be some logic, checking, if specific playroom has space for new players to join
                {
                    string[] substrings = message.Split("|");

                    Console.WriteLine($"[{ch.id}][{ch.ip}]Client requested to connect to playroom");
                    if (substrings.Length == 2)
                        PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), ch);
                    else if (substrings.Length == 3)
                        PlayroomManager.RequestFromClient_EnterPlayroom(Int32.Parse(substrings[1]), ch, substrings[2]);
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

                    PlayroomManager.RequestFromClient_StorePlayerPositionAndRotation(ch, position, rotation);
                }
                else if (message.StartsWith(CLIENT_DISCONNECTED_FROM_THE_PLAYROOM))
                {
                    Console.WriteLine($"[SERVER_MESSAGE]:Client [{ch.id}][{ch.ip}] disconnected from playroom");
                    string[] substrings = message.Split("|");
                    PlayroomManager.RequestFromClient_DisconnectFromPlayroom(int.Parse(substrings[1]), ch);
                }
                else if (message.StartsWith(SHOT_REQUEST))
                {
                    if (ch.player != null && ch.player.playroom != null)
                    {
                        ch.player.CheckAndMakeShot(message);
                    }
                }
                else if (message.StartsWith(JUMP_REQUEST))
                {
                    if (ch.player != null && ch.player.playroom != null)
                    {
                        ch.player.CheckAndMakeJump();
                    }
                }
                else if (message.StartsWith(PLAYER_DIED))
                {
                    if (ch.player != null && ch.player.playroom != null)
                    {
                        ch.player.PlayerDied(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
            Console.WriteLine($"IEndPoints amount: {udpEndpoints.Count}");
            int i = 1;
            foreach(UnassignedIPEndPoint a in udpEndpoints)
            {
                Console.WriteLine($"#{i} {a.endPoint.ToString()}");
            }
            Console.WriteLine();
        }

        #endregion
    }
}
