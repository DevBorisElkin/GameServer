using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GameServer
{
    public static class Util_UDP
    {
        #region stack UDP IPEndPoints and assign them when cient TCP connects

        public static List<UnassignedIPEndPoint> endpoints = new List<UnassignedIPEndPoint>();

        public static void TryToStoreEndPoint(IPEndPoint endPoint, string ip)
        {
            // check if there's end point for that ip
            if(endpoints.Count > 0)
            {
                foreach(var a in endpoints)
                {
                    if (a.ip.Equals(ip))
                    {
                        return;
                    }
                }
            }
            //Console.WriteLine($"[SERVER]: Stored IPEndPoint for client {ip}");
            endpoints.Add(new UnassignedIPEndPoint(endPoint, ip));
        }

        public static IPEndPoint TryToRetrieveEndPoint(string ip)
        {
            IPEndPoint result = null;
            UnassignedIPEndPoint unassigned = null;


            foreach (var a in endpoints)
            {
                if (a.ip.Equals(ip))
                {
                    unassigned = a;
                    break;
                }
            }

            if (unassigned != null)
            {
                result = unassigned.endPoint;
                endpoints.Remove(unassigned);
            }
            return result;
        }

        #endregion

    }

    public class UnassignedIPEndPoint
    {
        public IPEndPoint endPoint;
        public string ip;

        public UnassignedIPEndPoint(IPEndPoint _endPoint, string _ip)
        {
            endPoint = _endPoint;
            ip = _ip;
        }
    }
}
