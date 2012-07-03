using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace UtilsDLL
{
    class AWS_Utils
    {
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

            }
            catch (Exception e)
            {
                Console.WriteLine("Excpetion in Get_Launch_Specific_Data(). = " + e.Message);
                return false;
            }

            return true;
        }

        public static bool Get_My_IP_AND_DNS(out String ip,out String dns)
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
                ip = sr.ReadToEnd();
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
