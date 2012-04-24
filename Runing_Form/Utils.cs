using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Diagnostics;

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Web.Script.Serialization;

namespace Runing_Form
{
    public class Utils
    {
        public static String my_ip = "localhost";
        public static String public_dns = "localhost";

        public static Dictionary<String, String> CFG;
        public static bool Read_Cfg_File()
        {
            CFG = new Dictionary<string, string>();
            DirectoryInfo currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            String filePath = currentDir.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Extras" + Path.DirectorySeparatorChar + "ez3d.cfg";
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not locate config file in path "+ filePath);
                return false;
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                String line;
                char[] tokenizer = { '=' };
                while ((line = sr.ReadLine()) != null)
                {
                    String[] tokens = line.Split(tokenizer, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length != 2)
                    {
                        Console.WriteLine("illigle line in cfg file is:" + line);
                        return false;
                    }
                    CFG[tokens[0].Trim()] = tokens[1].Trim();
                }
            }
            return true;
        }

        public static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes
                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes
                = System.Convert.FromBase64String(encodedData);
            string returnValue =
               System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }

        public static bool Refresh_Rhino_GH_Data_From_Github()
        {
            // Pull data (all under ftproot/Rendering_Data from GitHub)
            DirectoryInfo currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            String batFileFolder = currentDir.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Extras";
            ProcessStartInfo psi = new ProcessStartInfo("pullData.bat");
            psi.WorkingDirectory = batFileFolder;
            Process p = Process.Start(psi);
            //p.WaitForInputIdle();
            p.WaitForExit();
            return true;
        }


        public static Boolean SendMail(String recepient, String subject, String msg)
        {
            MailMessage mail = new MailMessage("tamir@ez3d.co", recepient, subject, msg);
            SmtpClient client = new SmtpClient("smtp.gmail.com");
            NetworkCredential cred = new NetworkCredential("tamir@ez3d.co", "yardena12");
            client.EnableSsl = true;
            client.Credentials = cred;
            try
            {
                client.Send(mail);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return true;
        }



        public static bool Get_My_IP_AND_DNS()
        {
            Utils.my_ip = null;
            Utils.public_dns = null;


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
                Utils.my_ip = sr.ReadToEnd();
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
                Utils.public_dns = sr.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine("Excpetion in Get_My_IP_AND_DNS(). = " + e.Message);
                return false;
            }
            return true;
        }

    }

 
}
