using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace ServerMonitor
{
    class Program
    {
        public static void Shut_Down_Server()
        {
            ProcessStartInfo psi = new ProcessStartInfo("shutdown.exe", "/s /f");
            Process p = Process.Start(psi);
        }


        static void Main(string[] args)
        {
            if (!UtilsDLL.Dirs.get_all_relevant_dirs())
            {
                MessageBox.Show("ServerMonitor failed!!! - could not Get_Server_Launch_Time()");
                return;
            }

            DateTime launchTimeUTC;
            if(!UtilsDLL.AWS_Utils.Get_Server_Launch_Time(out launchTimeUTC))
            {
                MessageBox.Show("ServerMonitor failed!!! - could not Get_Server_Launch_Time()");
                return;
            }

            String imagesDirPath = UtilsDLL.Dirs.images_DirPath;

            DirectoryInfo dif = new DirectoryInfo(imagesDirPath);

            while (true)
            {
                DateTime utcNow = DateTime.Now.ToUniversalTime();
                TimeSpan fromLaunch = utcNow - launchTimeUTC;
                int minutesRunning = (int)fromLaunch.TotalMinutes + 3600;
                int minutesModulu = minutesRunning % 60;

                int minutesToRuningHour = 60 - (minutesModulu);
                TimeSpan timeFromLastFile = new TimeSpan(1,0,0);
                DateTime newest = new DateTime();
                int numOfFiles = 0;
                if (!UtilsDLL.Dirs.getUTC_Time_LastImage(dif,out newest,out numOfFiles))
                {
                    MessageBox.Show("getUTC_Time_LastImage() failed!!!");
                    return;
                }

                timeFromLastFile = utcNow - newest;

                if (minutesToRuningHour < 5)
                {
                    if ((timeFromLastFile.TotalMinutes > 5) || (numOfFiles == 0))
                    {
                        //MessageBox.Show("Decided to kill server because minutesToRuningHour=" + minutesToRuningHour.ToString() + " , timeFromLastFile=" + timeFromLastFile.ToString());
                        Shut_Down_Server();
                    }
                }

                String report = "minutesRunning=" + minutesRunning.ToString() + ", minutesModulu=" + minutesModulu.ToString() + ", minutesToRuningHour=" + minutesToRuningHour.ToString();
                if (numOfFiles == 0) report += " - NO new files in the last 5 mins";
                else report += ", timeFromLastFile=" + timeFromLastFile.Hours.ToString() + ":" + timeFromLastFile.Minutes.ToString() + ":" + timeFromLastFile.Seconds.ToString();
                Console.WriteLine(report);
                Thread.Sleep(60000);
            }
        }
    }
}
