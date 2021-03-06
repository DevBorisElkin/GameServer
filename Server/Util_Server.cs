using System;
using System.Net;
using System.Net.Sockets;
using static ServerCore.Server;
using static ServerCore.NetworkingMessageAttributes;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ServerCore
{
    public static class Util_Server
    {
        #region Util
        public static bool AlreadyHasThisClient(Socket socket)
        {
            if (TryToGetClientWithIp(GetRemoteIp(socket)) == null) return false;
            return true;
        }

        public static bool ViolatesLimitForTheSameIP(Socket socket)
        {
            int clientsWithSuchIPConnectedTotal = GetClientsWithSuchIPAmount(GetRemoteIp(socket));
            if (clientsWithSuchIPConnectedTotal <= 10) return false;
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
            Console.WriteLine($"[{DateTime.Now}][SERVER_ERROR]: Error getting first free id!");
            return -1;
        }
        public static ClientHandler TryToGetClientWithId(int id)
        {
            ClientHandler util;
            if (clients.TryGetValue(id, out util)) { return util; }
            else Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Didn't find client with id {id}");
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
        public static ClientHandler TryToGetClientWithUdpEndPoint(IPEndPoint remoteEndPoint)
        {
            foreach (var a in clients.Values)
            {
                if (a.udpEndPoint != null && a.udpEndPoint.Equals(remoteEndPoint)) return a;
            }
            return null;
        }

        public static int GetClientsWithSuchIPAmount(string ip)
        {
            int amount = 0;
            foreach (var a in clients.Values)
            {
                if (a.ip.Equals(ip)) amount++;
            }
            Console.WriteLine($"[{DateTime.Now}][SERVER_MESSAGE]: Clients with such ip {ip} detected: {amount}");
            return amount;
        }

        public static void DisposeAllClients()
        {
            foreach (var a in Server.clients.Values)
            {
                a.ShutDownClient(0);
            }
            clients = null;
        }

        public static IPAddress GetIpOfServer()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            return ipAddress;
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
        #endregion

        #region Util Connection
        public enum MessageProtocol { TCP, UDP }

        // Check if client connected
        public static bool SocketSimpleConnected(this ClientHandler ch)
        {
            return !((ch.tcpHandler.Poll(1000, SelectMode.SelectRead) && (ch.tcpHandler.Available == 0)) || !ch.tcpHandler.Connected);
        }
        public static bool SocketSimpleConnected(Socket tcpHandler)
        {
            return !((tcpHandler.Poll(1000, SelectMode.SelectRead) && (tcpHandler.Available == 0)) || !tcpHandler.Connected);
        }

        // Gets IP
        public static string GetRemoteIp(this ClientHandler ch)
        {
            string rawRemoteIP = ch.tcpHandler.RemoteEndPoint.ToString();
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
            return ch.tcpHandler.RemoteEndPoint.ToString();
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
            Console.WriteLine($"[{DateTime.Now}][SERVER]: Stored IPEndPoint for client {ip}");
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
                Console.WriteLine($"#{i} [{a.connectionID}][{a.ip}]");
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

        public enum ClientAccessLevel
        {
            LowestLevel = 0,
            Authenticated = 1
        }

        public enum ReasonOfDeath { ByOtherPlayer, Suicide }
        public enum DeathDetails { FellOutOfMap, TouchedSpikes }

        #region string compatibility check

        public static bool IsStringCompatible(string toCheck)
        {
            Regex rgx = new Regex("[^A-Za-z0-9_]");
            return !(rgx.IsMatch(toCheck));
        }

        public static bool StringStarstsFromNumberOrUnderscore(string toCheck)
        {
            string input = toCheck.Substring(0, 1);
            bool isDigitPresent = input.Any(c => char.IsDigit(c));
            bool startsWithUnderscore = toCheck.StartsWith("_");
            return (isDigitPresent || startsWithUnderscore);
        }

        public enum InputCompatibilityCheck { Success, Error_ContainsSpecialSymbols, Error_StartsWithNumberOrUnderscore, TooShort, TooLong, Empty, UnknownError}
        
        public static InputCompatibilityCheck CheckInputField(string stringToCheck, out string errorString, int MinLength = 5, int MaxLength = 12)
        {
            if (IsStringCompatible(stringToCheck) && stringToCheck.Length >= MinLength && stringToCheck.Length <= MaxLength && !StringStarstsFromNumberOrUnderscore(stringToCheck))
            {
                errorString = $"Everything is okay :)";
                return InputCompatibilityCheck.Success;
            }

            if (string.IsNullOrEmpty(stringToCheck))
            {
                errorString = $"input string is empty";
                return InputCompatibilityCheck.Empty;
            }
            if (!IsStringCompatible(stringToCheck))
            {
                errorString = $"input string contains special symbols";
                return InputCompatibilityCheck.Error_ContainsSpecialSymbols;
            }
            else if (stringToCheck.Length < MinLength)
            {
                errorString = $"input string does not exceed min length of {MinLength} symbols";
                return InputCompatibilityCheck.TooShort;
            }
            else if (stringToCheck.Length > MaxLength)
            {
                errorString = $"input string exceeds max allowed length of {MaxLength} symbols";
                return InputCompatibilityCheck.TooLong;
            }
            else if (StringStarstsFromNumberOrUnderscore(stringToCheck))
            {
                errorString = "input string starts with number or underscore";
                return InputCompatibilityCheck.Error_StartsWithNumberOrUnderscore;
            }
            else
            {
                errorString = "unknown error";
                return InputCompatibilityCheck.UnknownError;
            }
        } 
        #endregion

        #region PING CHECK

        // returns true if not belongs to check_connected
        public static bool EchoCheckConnectedMessage_TCP(string message, ClientHandler ch)
        {
            if (!message.Contains(CHECK_CONNECTED)) return true;
            try
            {
                string[] substrings = message.Split("|");
                if (substrings.Length > 1)
                {
                    int msgId = Int32.Parse(substrings[1]);
                    ch.SendMessageTcp($"{CHECK_CONNECTED_ECHO_TCP}|{msgId}{END_OF_FILE}");
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
            return false;
        }

        // returns true if not belongs to check_connected
        public static bool EchoCheckConnectedMessage_UDP(string message, ClientHandler ch)
        {
            if (!message.Contains(CHECK_CONNECTED)) return true;
            try
            {
                string[] substrings = message.Split("|");
                if (substrings.Length > 1)
                {
                    int msgId = Int32.Parse(substrings[1]);
                    UDP.SendMessageUdp($"{CHECK_CONNECTED_ECHO_UDP}|{msgId}{END_OF_FILE}", ch);
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
            return false;
        }

        #endregion

        #region Message Counter

        public static class MessageCounter
        {
            static List<MessageCounterInstance> counterInstances = new List<MessageCounterInstance>();
            public static void StartCounting(string key)
            {
                MessageCounterInstance counterInstance = new MessageCounterInstance(key);
                counterInstances.Add(counterInstance);
            }

            public static void UpdateCounter(string key)
            {
                MessageCounterInstance correctInstance = GetInstanceByKey(key);
                if(correctInstance != null)
                {
                    correctInstance.counter++;
                    if ((DateTime.Now - correctInstance.countInceptionDate).TotalMilliseconds > 1000)
                    {
                        Console.WriteLine($"[{key}] Messages count: {correctInstance.counter}");
                        correctInstance.countInceptionDate = DateTime.Now;
                        correctInstance.counter = 0;
                    }
                }
                else Console.WriteLine($"Error, counter instance for key {key} is null");
            }

            static MessageCounterInstance GetInstanceByKey(string key)
            {
                foreach (var a in counterInstances)
                    if (a.key == key) return a;
                return null;
            }

            class MessageCounterInstance
            {
                public string key;
                public DateTime countInceptionDate;
                public int counter;

                public MessageCounterInstance(string key)
                {
                    this.key = key;
                    countInceptionDate = DateTime.Now;
                    counter = 0;
                }
            }
        }
        #endregion
    }
}
