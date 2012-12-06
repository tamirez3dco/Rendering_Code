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
            if (UtilsDLL.AWS_Utils.is_aws)
            {
                host_ip = AWS_Utils.aws_ip;
                return true;
            }
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


        public static bool Get_DNS(out String dns)
        {
            if (UtilsDLL.AWS_Utils.is_aws)
            {
                dns = AWS_Utils.aws_dns;
                return true;
            }
            dns = Dns.GetHostName();
            return true;
        }
    }
}
