using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace Rhino_Restarter
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (!UtilsDLL.CFG.Turn_String_Into_CFG(args))
            {
                Console.WriteLine("UtilsDLL.CFG.Turn_String_Into_CFG(args[0]=" + args[0] + ") failed!!!");
                return;
            }

            String dns = null;
            IPAddress host_ip = null;
            if ((bool)UtilsDLL.CFG.Cfg_dict["is_AWS"])
            {
                String ip_str;
                if (!UtilsDLL.AWS_Utils.Get_My_IP_AND_DNS(out ip_str,out dns))
                {
                    Console.WriteLine("UtilsDLL.AWS_Utils.Get_My_IP_AND_DNS() failed!!!");
                    return;
                }
                host_ip = IPAddress.Parse(ip_str);
            }
            else
            {
                // Get my DNS
                UtilsDLL.Network_Utils.Get_DNS(out dns);
                // Get my external IP
                if (!UtilsDLL.Network_Utils.GetIP(out host_ip))
                {
                    Console.WriteLine("UtilsDLL.NetworkUtils.GetIP() failed!!!");
                    return;
                }
            }

            String my_Q_name = "restart_"+host_ip.ToString().Replace('.', '-');

            bool my_Q_found = false;
            String my_Q_url = String.Empty;
            String my_Q_arn = String.Empty;
            if (!UtilsDLL.SQS_Utils.Find_Q_By_name(my_Q_name, out my_Q_found, out my_Q_url,out my_Q_arn))
            {
                Console.WriteLine("UtilsDLL.SQS_Utils.Find_Q_By_name("+my_Q_name+",*,*) failed!!!");
                return;
            }


            if (!my_Q_found)
            {
                // Create Q
                if (!UtilsDLL.SQS_Utils.Create_Q(my_Q_name, out my_Q_url, out my_Q_arn))
                {
                    Console.WriteLine("UtilsDLL.SQS_Utils.Create_Q(my_Q_name="+my_Q_name+") failed!!!");
                    return;
                }
            }


            if (!UtilsDLL.SQS_Utils.Add_Q_Premissions_Everybody(my_Q_url))
            {
                Console.WriteLine("UtilsDLL.SQS_Utils.Delete_all_msgs_from_Q(my_Q_name=" + my_Q_name + ") failed!!!");
                return;
            }
            
            if (!UtilsDLL.SQS_Utils.Delete_all_msgs_from_Q(my_Q_url))
            {
                Console.WriteLine("UtilsDLL.SQS_Utils.Delete_all_msgs_from_Q(my_Q_name=" + my_Q_name + ") failed!!!");
                return;
            }


            // Attatch Q to SNS service
            //AmazonSimpleNotificationServiceClient sns_client = new AmazonSimpleNotificationServiceClient();
            //sns_client.
            String topic_name = "Restart_Rhinos";
            String topic_arn = String.Empty;
            bool topic_found = false;
            if (!UtilsDLL.SNS_Utils.Find_Topic_By_Name(topic_name, out topic_found, out topic_arn))
            {
                Console.WriteLine("SNS_Utils.Find_Topic_By_Name(topic_name="+topic_name+", out topic_found, out topic_arn) failed!!!");
                return;
            }

            bool listener_found = false;
            if (!UtilsDLL.SNS_Utils.Find_Listener_By_Q_arn(topic_name, my_Q_arn, out listener_found))
            {
                Console.WriteLine("UtilsDLL.SNS_Utils.List_All_Topics() failed!!!");
                return;
            }

            if (!listener_found)
            {
                if (!UtilsDLL.SNS_Utils.Add_SQS_Subscription(topic_arn, my_Q_arn))
                {
                    Console.WriteLine("UtilsDLL.SNS_Utils.Add_SQS_Subscription(topic_arn="+topic_arn+", my_Q_arn="+my_Q_arn+") failed!!!");
                    return;
                }
            }


            if (!UtilsDLL.SNS_Utils.Find_Listener_By_Q_arn(topic_name, my_Q_arn, out listener_found))
            {
                Console.WriteLine("UtilsDLL.SNS_Utils.List_All_Topics() failed!!!");
                return;
            }

            if (!listener_found)
            {
                Console.WriteLine("listener_found == false");
                return;
            }


            DateTime lastMsgTime = DateTime.Now;
            Console.WriteLine("Starting Restarter listening at time=" + lastMsgTime.ToString());
            DateTime last_restart_time = new DateTime();
            TimeSpan timeToWaitWhenRestarting = new TimeSpan(0, 2, 0);
            while (true)
            {
                Message msg;
                bool msg_found = false;
                if (!UtilsDLL.SQS_Utils.Get_Msg_From_Q(my_Q_url,out msg, out msg_found))
                {
                    Console.WriteLine("UtilsDLL.SQS_Utils.Get_Msg_From_Q(my_Q_url="+my_Q_url+",*,*) failed!!!");
                    return;
                }

                if (msg_found)
                {
                    if ((DateTime.Now - last_restart_time) > timeToWaitWhenRestarting)
                    {
                        Console.WriteLine("Performing RhinoRestart at time="+DateTime.Now.ToString());
                        last_restart_time = DateTime.Now;
                        Directory.SetCurrentDirectory(@"C:\inetpub\ftproot\Rendering_Code\Runing_Form");
                        ProcessStartInfo psi = new ProcessStartInfo(@"C:\inetpub\ftproot\Rendering_Code\Extras\RunDeploy.bat");
                        Process p = Process.Start(psi);
                    }
                    if (!UtilsDLL.SQS_Utils.Delete_Msg_From_Q(my_Q_url, msg))
                    {
                        Console.WriteLine("UtilsDLL.SQS_Utils.Delete_Msg_From_Q(my_Q_url=" + my_Q_url + ",msg) failed!!!");
                        return;
                    }
                }

                Thread.Sleep(2000);
                TimeSpan timeFromLastMsg = DateTime.Now - lastMsgTime;
                if (timeFromLastMsg.TotalSeconds > 60)
                {
                    Console.WriteLine("Resteting server is listening. Time=" + DateTime.Now.ToString() + ", lastRestartTime=" + last_restart_time.ToString());
                    lastMsgTime = DateTime.Now;
                }
            }
        }

    }
}