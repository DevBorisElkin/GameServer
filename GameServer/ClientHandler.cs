using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameServer.Util_Connection;
using static GameServer.NetworkingMessageAttributes;

namespace GameServer
{
    public class ClientHandler
    {
        public Socket handler;
        public IPEndPoint udpEndPoint;

        public ClientAccessLevel clientAccessLevel;
        public Player player;

        public int id;
        public string ip;

        double ms_connectedCheck = 3000;
        DateTime lastConnectedConfirmed;

        bool connected;

        public ClientHandler(Socket handler, int id)
        {
            clientAccessLevel = ClientAccessLevel.LowestLevel;
            this.handler = handler;
            this.id = id;
            ip = this.GetRemoteIp();

            connected = true;
            lastConnectedConfirmed = DateTime.Now;

            taskListener = new Task(ListenToMessages);
            taskListener.Start();
            connectionChecker = new Task(MaintainConnection);
            connectionChecker.Start();
        }

        #region basic methods
        // [LISTEN TO MESSAGES]
        Task taskListener;
        void ListenToMessages()
        {
            int errorMessages = 0;
            byte[] bytes = new byte[8192];
            string str;

            while (handler.Connected)
            {
                try
                {
                    str = ReadLine2(handler, bytes);
                    if (!str.Equals(""))
                    {
                        lastConnectedConfirmed = DateTime.Now;
                        Util_Server.OnMessageReceived(str, this, MessageProtocol.TCP);
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
        Task connectionChecker;
        void MaintainConnection()
        {
            while (connected)
            {
                Thread.Sleep(500);

                var msSinceLastConnectionConfirmed = (DateTime.Now - lastConnectedConfirmed).TotalMilliseconds;
                if(msSinceLastConnectionConfirmed > ms_connectedCheck)
                {
                    Console.WriteLine($"[SERVER_MESSAGE]: connection for client [{id}][{ip}] timed out");
                    ShutDownClient(3);
                }
                else
                {
                    SendMessageTcp(CHECK_CONNECTED+END_OF_FILE);
                }
            }
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
            try
            {
                byte[] dataToSend = Encoding.Unicode.GetBytes(message);
                handler.Send(dataToSend);
            }
            catch(Exception e)
            {
                //Console.WriteLine(e.Message + " " + e.StackTrace);
            }
        }

        public void ShutDownClient(int error = 0, bool removeFromClientsList = true)
        {
            connected = false;
            SendMessageTcp(CLIENT_DISCONNECTED+END_OF_FILE);

            Util_Server.OnClientDisconnected(this, error.ToString());

            try
            {
                if (handler.Connected)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Dispose();
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message + " " + e.StackTrace); }
            

            if (removeFromClientsList)
            {
                bool successfullyRemoved = Server.clients.Remove(this.id);
                //Console.WriteLine($"[SERVER_MESSAGE]: client [{id}][{ip}] was removed from clients list: [{successfullyRemoved}]");
            }
        }
        #endregion
    }

    public enum ClientAccessLevel
    {
        LowestLevel = 0,
        Authenticated = 1
    }
}
