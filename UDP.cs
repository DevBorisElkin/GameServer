using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public static class UDP
    {
        public static Server server;
        public static int portUdp;

        static IPEndPoint ipEndPointUdp;
        static Socket listenSocketUdp;

        public static bool listening;

        public static void StartUdpServer(int _port, Server _server)
        {
            portUdp = _port;
            server = _server;

            ipEndPointUdp = new IPEndPoint(IPAddress.Any, portUdp);
            listenSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            listening = true;
            Task udpListenTask = new Task(ListenUDP);
            udpListenTask.Start();
        }

        static EndPoint remote;
        #region Listen UPD - doesn't require established connection
        private static void ListenUDP()
        {
            try
            {
                listenSocketUdp.Bind(ipEndPointUdp);
                byte[] data = new byte[1024];
                remote = new IPEndPoint(IPAddress.Any, portUdp);

                int bytes;
                while (listening)
                {
                    StringBuilder builder = new StringBuilder();
                    do
                    {
                        bytes = listenSocketUdp.ReceiveFrom(data, ref remote);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (listenSocketUdp.Available > 0);

                    IPEndPoint remoteIp = remote as IPEndPoint;
                    string ip = ConnectionExtensions.GetRemoteIp(remoteIp);
                    ClientHandler clientToBind = server.TryToGetClientWithIp(ip);

                    if (clientToBind == null)
                    {
                        Console.WriteLine($"[SYSTEM_ERROR]: didn't find client in clients list with ip {ip}");
                        continue;
                    }

                    // on first UDP message bind IPEndPoint to selected ClientHandler
                    if (builder.ToString().StartsWith("init_udp"))
                    {
                        clientToBind.udpEndPoint = remoteIp;
                        Console.WriteLine($"[SYSTEM_MESSAGE]: initialized IPEndPoint for UDP messaging of client [{clientToBind.id}][{clientToBind.ip}]");
                    }
                    else
                    {
                        server.OnMessageReceived(builder.ToString(), clientToBind, Server.MessageProtocol.UDP);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        public static void SendMessageUdp(string message, IPEndPoint remoteIp)
        {
            if (remoteIp != null)
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                listenSocketUdp.SendTo(data, remoteIp);
            }
            else
            {
                Console.WriteLine("Remote end point has not beed defined yet");
            }

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
