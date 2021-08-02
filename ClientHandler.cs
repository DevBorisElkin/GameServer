using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class ClientHandler
    {
        public Server server;
        public Socket handler;
        public IPEndPoint udpEndPoint;

        public Player player;

        public int id;
        public string ip;

        public ClientHandler(Server server, Socket handler, int id)
        {
            this.server = server;
            this.handler = handler;
            this.id = id;
            ip = this.GetRemoteIp();

            taskListener = new Task(ListenToMessages);
            taskListener.Start();
        }

        #region basic methods
        // [LISTEN TO MESSAGES]
        Task taskListener;
        void ListenToMessages()
        {
            int errorMessages = 0;
            byte[] bytes = new byte[1024];
            string str;

            while (handler.Connected)
            {
                try
                {
                    str = ReadLine2(handler, bytes);
                    if (!str.Equals(""))
                    {
                        server.OnMessageReceived(str, this, Server.MessageProtocol.TCP);
                    }
                    else if (str.Equals(""))
                    {
                        errorMessages++;
                        if (errorMessages > 25)
                        {
                            ShutDownClient(1);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    ShutDownClient(2);  // usually never called, but for safety
                    break;
                }
            }
        }

        string ReadLine(Socket reciever, byte[] buffer)
        {
            int bytesRec = reciever.Receive(buffer);
            string data = Encoding.ASCII.GetString(buffer, 0, bytesRec);
            return data;
        }

        string ReadLine2(Socket reciever, byte[] buffer)
        {
            StringBuilder builder = new StringBuilder();
            int bytes = 0; // amount of received bytes

            do
            {
                bytes = reciever.Receive(buffer);
                builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
            }
            while (reciever.Available > 0);

            return builder.ToString();
        }
        public void SendMessageTcp(string message)
        {
            byte[] dataToSend = Encoding.Unicode.GetBytes(message);
            handler.Send(dataToSend);
        }

        public void ShutDownClient(int error = 0, bool removeFromClientsList = true)
        {
            server.OnClientDisconnected(this, error.ToString());

            if (handler.Connected)
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Dispose();
            }

            if (removeFromClientsList) server.clients.Remove(this.id);
        }
        #endregion



        //public void SendIntoGame_PlayerConnected(string nickname)
        //{
        //    player = new Player(id, nickname, Vector3.Zero);
        //
        //    server.SendMessageToAllClients()
        //}
    }
}
