using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace UtilsDLL
{
    public class AWS_Utils
    {
        public static bool is_aws = false;
        public static IPAddress aws_ip;
        public static String aws_dns = String.Empty;

        public static bool Get_Launch_Specific_Data(out String user_data)
        {
            user_data = String.Empty;
            try
            {
                string sURL = "http://169.254.169.254/latest/user-data/";
                WebRequest wrGETURL;
                wrGETURL = WebRequest.Create(sURL);
                WebProxy myProxy = new WebProxy("myproxy", 80);
                myProxy.BypassProxyOnLocal = true;

                Stream objStream;
                objStream = wrGETURL.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(objStream);
                user_data = sr.ReadToEnd();

                Console.WriteLine("user_Data_String=" + user_data);
                if (!Get_My_IP_AND_DNS(out UtilsDLL.AWS_Utils.aws_ip, out UtilsDLL.AWS_Utils.aws_dns))
                {
                    Console.WriteLine("Get_My_IP_AND_DNS() failed!!");
                    return false;
                }
                is_aws = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Excpetion in Get_Launch_Specific_Data(). = " + e.Message);
                is_aws = false;
                return false;
            }

            return true;
        }

        public static bool Get_My_IP_AND_DNS(out IPAddress ip,out String dns)
        {
            ip = null;
            dns = null;


            try
            {
                string sURL = "http://169.254.169.254/latest/meta-data/public-ipv4";
                WebRequest wrGETURL;
                wrGETURL = WebRequest.Create(sURL);
                WebProxy myProxy = new WebProxy("myproxy", 80);
                myProxy.BypassProxyOnLocal = true;

                Stream objStream;
                objStream = wrGETURL.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(objStream);
                if (!IPAddress.TryParse(sr.ReadToEnd(), out ip))
                {
                    Console.WriteLine("(!IPAddress.TryParse(sr.ReadToEnd(), out ip))failed!!!");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Excpetion in Get_My_IP_AND_DNS(). = " + e.Message);
                return false;
            }

            try
            {
                string sURL = "http://169.254.169.254/latest/meta-data/public-hostname";
                WebRequest wrGETURL;
                wrGETURL = WebRequest.Create(sURL);
                WebProxy myProxy = new WebProxy("myproxy", 80);
                myProxy.BypassProxyOnLocal = true;

                Stream objStream;
                objStream = wrGETURL.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(objStream);
                dns = sr.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine("Excpetion in Get_My_IP_AND_DNS(). = " + e.Message);
                return false;
            }
            return true;
        }

        public static bool Get_Server_Launch_Time(out DateTime utcLaunch)
        {
            utcLaunch = new DateTime();
            try
            {
                string sURL = "http://169.254.169.254/latest/dynamic/instance-identity/document";
                WebRequest wrGETURL;
                wrGETURL = WebRequest.Create(sURL);
                WebProxy myProxy = new WebProxy("myproxy", 80);
                myProxy.BypassProxyOnLocal = true;

                Stream objStream;
                objStream = wrGETURL.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(objStream);
                String responseString = sr.ReadToEnd();
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var jsonObject = serializer.DeserializeObject(responseString) as Dictionary<string, object>;
                Dictionary<String, Object> jsonDict = (Dictionary<String, Object>)jsonObject;
                String utcKey = "pendingTime";
                if (!jsonDict.ContainsKey(utcKey))
                {
                    Console.WriteLine("(!jsonDict.ContainsKey(" + utcKey + ")) in Get_Server_Launch_Time()");
                    return false;
                }
                String utcTimeString = (String)jsonDict[utcKey];
                bool res = DateTime.TryParse(utcTimeString, out utcLaunch);

            }
            catch (Exception e)
            {
                Console.WriteLine("Excpetion in Get_Server_Launch_Time(). = " + e.Message);
                return false;
            }



            return true;
        }


    }
}
