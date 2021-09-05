using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static ServerCore.Util_Server;
using static ServerCore.NetworkingMessageAttributes;

namespace ServerCore
{
    public static class UDP
    {
        public static int portUdp;

        static IPEndPoint ipEndPointUdp;
        static Socket listenSocketUdp;

        public static bool listening;

        public static void StartUdpServer(int _port)
        {
            portUdp = _port;

            ipEndPointUdp = new IPEndPoint(IPAddress.Any, portUdp);
            listenSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            listening = true;
            Task udpListenTask = new Task(ListenUDP);
            udpListenTask.Start();
        }

        #region Listen UPD - doesn't require established connection
        private static void ListenUDP()
        {
            try
            {
                listenSocketUdp.Bind(ipEndPointUdp);
                byte[] data = new byte[1024];
                EndPoint remote; remote = new IPEndPoint(IPAddress.Any, portUdp);

                int bytes;
                while (listening)
                {
                    try
                    {
                        StringBuilder builder = new StringBuilder();
                        do
                        {
                            bytes = listenSocketUdp.ReceiveFrom(data, ref remote);
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        while (listenSocketUdp.Available > 0);

                        string message = builder.ToString();
                        if (message.StartsWith(INIT_UDP))
                        {
                            string[] substrings = message.Split("|");
                            int clientIdInClientsList = Int32.Parse(substrings[1]);
                            ClientHandler client = TryToGetClientWithId(clientIdInClientsList);
                            if(client != null)
                            {
                                if(client.udpEndPoint == null)
                                {
                                    IPEndPoint _remoteIp = remote as IPEndPoint;
                                    client.udpEndPoint = _remoteIp;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[SERVER_MESSAGE][{DateTime.Now}]: couldn't initialize udp end point for client with local id [{clientIdInClientsList}]");
                                continue;
                            }
                        }
                        else
                        {
                            ClientHandler client = TryToGetClientWithUdpEndPoint(remote as IPEndPoint);
                            if(client != null)
                            {
                                OnMessageReceived(message, client, MessageProtocol.UDP);
                            }
                            else
                            {
                                Console.WriteLine($"[SERVER_MESSAGE][{DateTime.Now}]: couldn't find client with given IPEndPoint");

                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"[{DateTime.Now}] {e.Message} ||| {e.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] " + ex.Message);
            }
            finally
            {
                CloseUdp();
            }
        }
        private static void CloseUdp()
        {
            listening = false;
            if (listenSocketUdp != null)
            {
                listenSocketUdp.Shutdown(SocketShutdown.Both);
                listenSocketUdp.Close();
                listenSocketUdp = null;
            }
        }
        #endregion

        public static void SendMessageUdp(string message, ClientHandler ch)
        {
            if (!TryToRetrieveEndPoint(ch)) return;

            if (listenSocketUdp != null)
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                listenSocketUdp.SendTo(data, ch.udpEndPoint);
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] Remote end point has not beed defined yet, or listenSocketUdp is equal to null");
            }

        }

        public static bool TryToRetrieveEndPoint(ClientHandler ch)
        {
            if (ch.udpEndPoint == null)
            {
                ch.udpEndPoint = Util_Server.TryToRetrieveEndPoint(ch.ip);
                if (ch.udpEndPoint == null)
                {
                    Console.WriteLine($"[{DateTime.Now}][SERVER_ERROR]: Unable to interact with client via UDP" +
                        $" - failed to assign UDP IPEndPoint to client [{ch.connectionID}][{ch.ip}]");
                    return false;
                }
                Console.WriteLine($"[{DateTime.Now}][SYSTEM_MESSAGE]: 2) Retrieved IPEndPoint for UDP messaging of client [{ch.connectionID}][{ch.ip}]");
                return true;
            }
            else return true;
        }

        static void WriteAddressToConsole()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            Console.WriteLine("Address = " + ipAddress);
            Console.WriteLine("_____________________\n");
        }
    }
}
