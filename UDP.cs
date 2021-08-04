using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static GameServer.Util_Connection;
using static GameServer.Util_Server;

namespace GameServer
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

                        IPEndPoint remoteIp = remote as IPEndPoint;
                        string ip = Util_Connection.GetRemoteIp(remoteIp);
                        ClientHandler clientToBind = TryToGetClientWithIp(ip);

                        if (clientToBind == null)
                        {
                            //Console.WriteLine($"[SYSTEM_ERROR]: didn't find client in clients list with ip {ip}");
                            Util_UDP.AddEndPoint(remoteIp, ip);
                            continue;
                        }else if (builder.ToString().StartsWith("init_udp"))
                        {
                            if (clientToBind.udpEndPoint != null) continue;
                            clientToBind.udpEndPoint = remoteIp;
                            Console.WriteLine($"[SYSTEM_MESSAGE]: initialized IPEndPoint for UDP messaging of client [{clientToBind.id}][{clientToBind.ip}]");
                        }
                        else
                        {
                            OnMessageReceived(builder.ToString(), clientToBind, MessageProtocol.UDP);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"{e.Message} ||| {e.StackTrace}");
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
            if (remoteIp != null && listenSocketUdp != null)
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                listenSocketUdp.SendTo(data, remoteIp);
            }
            else
            {
                Console.WriteLine("Remote end point has not beed defined yet, or listenSocketUdp is equal to null");
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
