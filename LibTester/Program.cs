using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino4;
using System.Diagnostics;
using System.Threading;

namespace LibTester
{
    class Program
    {
        static void Main(string[] args)
        {
            // kill all current Rhino4.exe processes
            Process[] procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Killing " + procs.Length + " previous Rhino processes");
            foreach (Process p in procs) { p.Kill(); }
            Thread.Sleep(1000);
            procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine(procs.Length + " previous Rhino processes remaind alive");
            UtilsDLL.Dirs.get_all_relevant_dirs();

/*

            Rhino5Application rhino_app = new Rhino5Application();
            rhino_app.Visible = 1;
            rhino_app.RunScript("_Grasshopper", 0);
            dynamic grasshopper = rhino_app.GetPlugInObject("b45a29b1-4343-4035-989e-044e8580d9cf", "00000000-0000-0000-0000-000000000000") as dynamic;
            grasshopper.OpenDocument(@"C:\inetpub\ftproot\Rendering_Data\GH_Def_files\iPhone-frames-trial-release.gh");
            //bool res = grasshopper.AssignDataToParameter("Num", (Object)0.5);
*/
            UtilsDLL.Rhino.Rhino_Wrapper rhino_wrapper;
            if (!UtilsDLL.Rhino.start_a_SingleRhino("rings.3dm", true, out rhino_wrapper))
            {
                Console.WriteLine("Basa");
                return;
            }

            Dictionary<String, Object> dic = new Dictionary<string,object>();

            for (int i = 0; i < 4; i++)
            {
                dic["par1"] = 5 - i;
                dic["par2"] = 2 + i;

            bool res1 = UtilsDLL.Rhino.Set_GH_Params_To_TXT_File(rhino_wrapper, dic);

            bool res2 = UtilsDLL.Rhino.Open_GH_File(rhino_wrapper, UtilsDLL.Dirs.GH_DirPath + "/iPhone-frames-trial-release.gh");

            bool res3 = UtilsDLL.Rhino.Solve_And_Bake(rhino_wrapper, "Bakery");

            bool res4 = UtilsDLL.Rhino.Render(rhino_wrapper, "Render", new System.Drawing.Size(200, 200), @"C:\Temp\hope_" + i +".jpg");

            }
            //            grasshopper.RunSolver(true);

//            Object objRes = grasshopper.BakeDataInObject("G");

/*
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
 */ 
        }
    }
}
