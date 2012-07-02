using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace UtilsDLL
{
    public class Network_Utils
    {
        public static bool GetIP(out IPAddress host_ip)
        {
            host_ip = null;
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    host_ip = ip;
                }
            }
            return (host_ip != null);
        }

    }
}
