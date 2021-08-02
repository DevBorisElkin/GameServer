using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameServer.NetworkingMessageAttributes;

namespace GameServer
{
    public class Server
    {
        public string ip;
        public int port = 8384;
        public Dictionary<int, ClientHandler> clients;

        public bool serverActive;

        Socket handler;
        Socket listenSocket;

        bool playroomActive;

        #region Delegates
        public delegate void OnServerStartedDelegate();
        public event OnServerStartedDelegate OnServerStartedEvent;

        public delegate void OnServerShutDownDelegate();
        public event OnServerShutDownDelegate OnServerShutDownEvent;

        public delegate void OnClientConnectedDelegate(ClientHandler client);
        public event OnClientConnectedDelegate OnClientConnectedEvent;

        public delegate void OnClientDisconnectedDelegate(ClientHandler client, string error);
        public event OnClientDisconnectedDelegate OnClientDisconnectedEvent;

        public delegate void OnMessageReceivedDelegate(string message, ClientHandler ch, MessageProtocol mp);
        public event OnMessageReceivedDelegate OnMessageReceivedEvent;

        public enum MessageProtocol { TCP, UDP }


        void OnServerStarted() { OnServerStartedEvent?.Invoke(); }
        void OnServerShutDown() { OnServerShutDownEvent?.Invoke(); }
        public void OnClientConnected(ClientHandler client) { OnClientConnectedEvent?.Invoke(client); }
        public void OnClientDisconnected(ClientHandler client, string error) { OnClientDisconnectedEvent?.Invoke(client, error); }
        public void OnMessageReceived(string message, ClientHandler ch, MessageProtocol mp) { OnMessageReceivedEvent?.Invoke(message, ch, mp); }
        #endregion

        // [START SERVER]
        public void StartServer(int port)
        {
            this.port = port;
            ip = GetIpOfServer().ToString();

            clients = new Dictionary<int, ClientHandler>();

            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.Bind(ipEndPoint);
            listenSocket.Listen(5);
            serverActive = true;

            OnServerStarted();

            Task listenToConnectionsTask = new Task(ListenToNewConnections);
            listenToConnectionsTask.Start();

        }
        // [LISTEN TO CONNECTIONS]
        void ListenToNewConnections()
        {
            try
            {
                while (serverActive)
                {
                    handler = listenSocket.Accept();
                    if (!this.AlreadyHasThisClient(handler))
                    {
                        int clientId = GetFirstFreeId();
                        ClientHandler client = new ClientHandler(this, handler, clientId);
                        AddClient(client, clientId);
                    }
                    else
                    {
                        Console.WriteLine($"[SERVER_MESSAGE] reject repetetive connection from {ConnectionExtensions.GetRemoteIp(handler)}");
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

        // Manage playroom 1
        #region PlayRoom 1
        Task managingPlayRoom;
        public void TurnOn_Playroom()
        {
            int playersInPlayRoom = 0;
            foreach(var a in clients.Values)
            {
                if (a.player != null) playersInPlayRoom++;
            }
            if(playersInPlayRoom <= 0)
            {
                Console.WriteLine($"Not enough players in play room to turn on Managing of it. [{playersInPlayRoom}]");
                return;
            }

            playroomActive = true;
            managingPlayRoom = new Task(ManagePlayroom);
            managingPlayRoom.Start();
        }
        public void TurnOff_Playroom()
        {
            int playersInPlayRoom = 0;
            foreach (var a in clients.Values)
            {
                if (a.player != null) playersInPlayRoom++;
            }
            if (playersInPlayRoom <= 0)
            {
                playroomActive = false;
            }
            
        }

        void ManagePlayroom()
        {
            // here we will send room's position and rotation to all players connected to that room

            while (playroomActive) // sending data 10 times a second
            {
                foreach(var a in clients.Values)
                {
                    if (a.player == null) continue;
                    UDP.SendMessageUdp(GenerateStringSendingPlayersOtherPlayersPositions(a), a.udpEndPoint);
                }

                Thread.Sleep(100);
            }


        }

        // |nickname,ip,position,rotation@nickname,ip,position,rotation@enc..."
        string GenerateStringSendingPlayersOtherPlayersPositions(ClientHandler exceptThisOne)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MESSAGE_TO_ALL_CLIENTS_ABOUT_PLAYERS_DATA_IN_PLAYROOM + "|");
            foreach (var a in clients.Values)
            {
                if (a.player == null) continue;
                if (a == exceptThisOne) continue;

                sb.Append($"{a.player.username},{a.ip},{a.player.position},{a.player.rotation}@");
            }
            string message = sb.ToString();
            int lastIndexOfDog = message.LastIndexOf("@");
            if(message.Length == lastIndexOfDog)
            {
                message = message.Remove(lastIndexOfDog, 1);
            }

            return message;
        }
        #endregion PlayRoom 1
        // [SHUT DOWN SERVER]
        public void ShutDownServer()
        {
            serverActive = false;
            if (handler != null)
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            DisposeAllClients();
            OnServerShutDown();
        }
        // [ADD CLIENT]
        void AddClient(ClientHandler client, int id)
        {
            OnClientConnected(client);
            clients[id] = client;
        }
        // [REMOVE CLIENT]
        public void DisconnectClient(ClientHandler client)
        {
            client.ShutDownClient();
        }

        #region Util
        int GetFirstFreeId()
        {
            ClientHandler util;
            for (int i = 1; i < 10000; i++)
            {
                if (!clients.TryGetValue(i, out util))
                {
                    return i;
                }
            }
            Console.WriteLine("Error getting first free id!");
            return -1;
        }
        public ClientHandler TryToGetClientWithId(int id)
        {
            ClientHandler util;
            if (clients.TryGetValue(id, out util)) { return util; }
            else Console.WriteLine($"Error getting client with id {id}");
            return null;
        }
        public ClientHandler TryToGetClientWithIp(string ip)
        {
            ClientHandler util;
            for (int i = 1; i <= clients.Count; i++)
            {
                if (clients.TryGetValue(i, out util))
                {
                    if (util.ip.Equals(ip))
                    {
                        return util;
                    }
                }
            }
            return null;
        }

        void DisposeAllClients()
        {
            for (int i = 1; i <= clients.Count; i++)
            {
                clients[i].ShutDownClient(0, false);
            }
            clients = null;
        }

        // [SEND MESSAGE]
        public void SendMessageToAllClients(string message, MessageProtocol mp = MessageProtocol.TCP, ClientHandler clientToIgnore = null)
        {
            if (mp.Equals(MessageProtocol.TCP))
            {
                foreach(var a in clients.Values)
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
                    UDP.SendMessageUdp(message, a.udpEndPoint);
                }
            }
        }
        public void SendMessageToAllClientsInPlayroom(string message, MessageProtocol mp = MessageProtocol.TCP, ClientHandler clientToIgnore = null)
        {
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
                    UDP.SendMessageUdp(message, a.udpEndPoint);
                }
            }
        }
        public void SendMessageToClient(string message, string ip)
        {
            ClientHandler clientHandler = TryToGetClientWithIp(ip);
            if (clientHandler != null) clientHandler.SendMessageTcp(message);
        }
        public void SendMessageToClient(string message, int id)
        {
            ClientHandler clientHandler = TryToGetClientWithId(id);
            if (clientHandler != null) clientHandler.SendMessageTcp(message);
        }
        public void SendMessageToClient(string message, ClientHandler client)
        {
            client.SendMessageTcp(message);
        }


        IPAddress GetIpOfServer()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            return ipAddress;
        }
        #endregion
    }
}
