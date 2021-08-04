using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    /// <summary>
    /// Represents static methods that help maintaining connection
    /// </summary>
    public static class Util_Connection
    {
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
    }
}
