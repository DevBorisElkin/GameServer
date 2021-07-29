using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class ClientHandler
    {
        Server server;
        public Socket handler;
        public IPEndPoint udpEndPoint;

        public int id;
        public string ip;

        Task taskListener;
        public ClientHandler(Server server, Socket handler, int id)
        {
            this.server = server;
            this.handler = handler;
            this.id = id;
            ip = this.GetRemoteIp();

            taskListener = new Task(ListenToMessages);
            taskListener.Start();
        }

        int errorMessages = 0;
        // [LISTEN TO MESSAGES]
        void ListenToMessages()
        {
            byte[] bytes = new byte[1024];
            string str;

            while (true)
            {
                try
                {
                    str = ReadLine2(handler, bytes);
                    if (!str.Equals(""))
                    {
                        server.OnMessageReceived(str, id, ip, Server.MessageProtocol.TCP);
                    }
                    else
                    {
                        errorMessages++;
                        if (errorMessages > 100)
                        {
                            ShutDownClient(1);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    ShutDownClient(2);
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
            while (handler.Available > 0);

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
            handler.Dispose();
            if (removeFromClientsList) server.clients.Remove(this.id);
            taskListener.Dispose();
        }
    }
}
