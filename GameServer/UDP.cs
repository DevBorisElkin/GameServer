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

                        // we can't parse UDP messages from client if TCP connection is not yet established
                        if (clientToBind == null)
                        {
                            Util_UDP.TryToStoreEndPoint(remoteIp, ip);
                            continue;
                        }
                        // we can't parse UDP messages from client if IPEndPoint for UDP is not set yet
                        else if (clientToBind.udpEndPoint == null)
                        {
                            clientToBind.udpEndPoint = remoteIp;
                            //Console.WriteLine($"[SYSTEM_MESSAGE]: 1) initialized IPEndPoint for UDP messaging of client [{clientToBind.id}][{clientToBind.ip}]");
                        }
                        // everything is OK, we can work with message
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

        public static void SendMessageUdp(string message, ClientHandler ch)
        {
            if (ch.udpEndPoint == null)
            {
                ch.udpEndPoint = Util_UDP.TryToRetrieveEndPoint(ch.ip);
                if (ch.udpEndPoint == null)
                {
                    Console.WriteLine($"[SERVER_ERROR]: Can't send an UDP message - failed to assign UDP IPEndPoint to client [{ch.id}][{ch.ip}]");
                    return;
                }
                //Console.WriteLine( $"[SYSTEM_MESSAGE]: 2) initialized IPEndPoint for UDP messaging of client [{ch.id}][{ch.ip}]");
            }

            if (listenSocketUdp != null)
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                listenSocketUdp.SendTo(data, ch.udpEndPoint);
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
