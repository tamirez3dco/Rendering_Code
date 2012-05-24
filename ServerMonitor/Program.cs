using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace ServerMonitor
{
    class Program
    {


        static void Main(string[] args)
        {
            DateTime launchTimeUTC;
            if(!Runing_Form.Utils.Get_Server_Launch_Time(out launchTimeUTC))
            {
                MessageBox.Show("ServerMonitor failed!!! - could not Get_Server_Launch_Time()");
                return;
            }

            String imagesDirPath;
            if (!Runing_Form.Runing_Form.get_tempImages_files_Dir(out imagesDirPath))
            {
                MessageBox.Show("ServerMonitor failed!!! - could not get_tempImages_files_Dir()");
                return;
            }
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
                if (!Runing_Form.Utils.getUTC_Time_LastImage(dif, out newest))
                {
                    MessageBox.Show("getUTC_Time_LastImage() failed!!!");
                    return;
                }

                timeFromLastFile = utcNow - newest;

                if (minutesToRuningHour < 5)
                {
                    if (timeFromLastFile.TotalMinutes > 5)
                    {
                        //MessageBox.Show("Decided to kill server because minutesToRuningHour=" + minutesToRuningHour.ToString() + " , timeFromLastFile=" + timeFromLastFile.ToString());
                        Runing_Form.Utils.Shut_Down_Server();
                    }
                }

                Console.WriteLine("minutesRunning=" + minutesRunning.ToString() + ", minutesModulu="+minutesModulu.ToString()+", minutesToRuningHour=" + minutesToRuningHour.ToString() + " , timeFromLastFile=" + timeFromLastFile.ToString());
                Thread.Sleep(60000);
            }
        }
    }
}
