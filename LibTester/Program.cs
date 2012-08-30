using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilsDLL;
using System.Diagnostics;
using System.Threading;
namespace LibTester
{
    class Program
    {
        static void Main(string[] args)
        {


            UtilsDLL.Dirs.get_all_relevant_dirs();

            UtilsDLL.Rhino.Rhino_Wrapper wrapper;

            // kill all current Rhino4.exe processes
            Process[] procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Killing " + procs.Length + " previous Rhino processes");
            foreach (Process p in procs) { p.Kill(); }
            Thread.Sleep(1000);
            procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine(procs.Length + " previous Rhino processes remaind alive");

            bool createRes = UtilsDLL.Rhino.start_a_SingleRhino("rings.3dm", true, out wrapper);

            bool loadRes = UtilsDLL.Rhino.Open_GH_File(wrapper, @"C:\inetpub\ftproot\Rendering_Data\GH_Def_files\test1.ghx");

            Dictionary<String,Object> dict = new Dictionary<string,object>();
            dict["4e459553-8255-4da2-915f-ebd9ee1c192b"] = 0.4;
            bool changeRes = UtilsDLL.Rhino.Set_GH_Params(wrapper,"M",dict);

            //UtilsDLL.Rhino.b
        }
    }
}
