using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

public static class ClientHandlerExtensions
{
    public static bool SocketSimpleConnected(this ClientHandler ch)
    {
        return !((ch.handler.Poll(1000, SelectMode.SelectRead) && (ch.handler.Available == 0)) || !ch.handler.Connected);
    }

    public static string GetRemoteIp(this ClientHandler ch)
    {
        string rawRemoteIP = ch.handler.RemoteEndPoint.ToString();
        int dotsIndex = rawRemoteIP.LastIndexOf(":");
        string remoteIP = rawRemoteIP.Substring(0, dotsIndex);
        return remoteIP;
    }
    public static string GetRemoteIpAndPort(this ClientHandler ch)
    {
        return ch.handler.RemoteEndPoint.ToString();
    }
}
