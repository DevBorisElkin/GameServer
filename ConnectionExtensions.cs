using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    public static class ConnectionExtensions
    {
        public static bool SocketSimpleConnected(this ClientHandler ch)
        {
            return !((ch.handler.Poll(1000, SelectMode.SelectRead) && (ch.handler.Available == 0)) || !ch.handler.Connected);
        }
        public static bool SocketSimpleConnected(Socket tcpHandler)
        {
            return !((tcpHandler.Poll(1000, SelectMode.SelectRead) && (tcpHandler.Available == 0)) || !tcpHandler.Connected);
        }

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

        public static string GetRemoteIpAndPort(this ClientHandler ch)
        {
            return ch.handler.RemoteEndPoint.ToString();
        }

        public static string GetRemoteIpAndPort(Socket tcpHandler)
        {
            return tcpHandler.RemoteEndPoint.ToString();
        }

        public static bool AlreadyHasThisClient(this Server server, Socket socket)
        {
            if (server.TryToGetClientWithIp(GetRemoteIp(socket)) == null) return false;
            return true;
        }
    }
}
