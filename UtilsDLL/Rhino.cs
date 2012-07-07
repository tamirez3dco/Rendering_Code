using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino4;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace UtilsDLL
{
    public class Rhino
    {
        public class Rhino_Wrapper
        {
            public Rhino5Application rhino_app;
            public int rhino_pid;
            public DateTime creationTime;
            public DateTime killTime;
            public dynamic grasshopper;
        }


        public enum Cycle_Result
        {
            NO_MSG,
            ERROR,
            SUCCESS
        }
        public static Cycle_Result single_cycle(String Q_name, TimeSpan timeout)
        {
            // get SQS Qs urls

            // try read SQS req msg

            // if no msg return true


            // SQS 1
            // delete all, set layer etc...

            // enter params

            // script/grasshoppper

            // render

            // S3

            //SQS 2
            return Cycle_Result.SUCCESS;
        }

        public static bool start_a_SingleRhino(String sceneFile_name, bool rhino_visible, out UtilsDLL.Rhino.Rhino_Wrapper newRhino)
        {
            newRhino = new UtilsDLL.Rhino.Rhino_Wrapper();

            Process[] procs_before = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Starting Rhino at " + DateTime.Now);
            newRhino.rhino_app = new Rhino5Application();
            newRhino.rhino_app.ReleaseWithoutClosing = 1;
            newRhino.rhino_app.Visible = rhino_visible ? 1 : 0;
            if (newRhino == null)
            {
                Console.WriteLine("rhino == null");
                return false;
            }
            for (int tries = 0; tries < 200; tries++)
            {
                if (newRhino.rhino_app.IsInitialized() == 1)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            Process[] procs_after = Process.GetProcessesByName("Rhino4");
            List<int> pids_before = new List<int>();
            List<int> new_pids = new List<int>();
            foreach (Process p in procs_before) pids_before.Add(p.Id);
            foreach (Process p in procs_after)
            {
                if (!pids_before.Contains(p.Id))
                {
                    new_pids.Add(p.Id);
                }
            }

            if (new_pids.Count != 1)
            {
                Console.WriteLine("ERROR ! - (new_pids.Count != 1 =" + new_pids.Count);
                Console.WriteLine("procs_before=:");
                foreach (Process p in procs_before) Console.WriteLine(p.Id);
                Console.WriteLine("procs_after=:");
                foreach (Process p in procs_after) Console.WriteLine(p.Id);
                return false;
            }
            else
            {
                newRhino.rhino_pid = new_pids[0];
            }

            Console.WriteLine("Starting Grasshopper at " + DateTime.Now);
            newRhino.rhino_app.RunScript("_Grasshopper", 0);
            Thread.Sleep(1000);

            newRhino.grasshopper = newRhino.rhino_app.GetPlugInObject("b45a29b1-4343-4035-989e-044e8580d9cf", "00000000-0000-0000-0000-000000000000") as dynamic;
            if (newRhino.grasshopper == null)
            {
                Console.WriteLine("ERROR!!: (grasshopper == null)");
                return false;
            }
            newRhino.grasshopper.HideEditor();

            String sceneFilePath = UtilsDLL.Dirs.scenes_DirPath + Path.DirectorySeparatorChar + sceneFile_name;
            if (!File.Exists(sceneFilePath))
            {
                Console.WriteLine("ERROR!!: sceneFilePath="+sceneFilePath+" does not exists");
                return false;
            }

            String openCommand = "-Open " + sceneFilePath;
            int openCommandRes = newRhino.rhino_app.RunScript(openCommand, 1);

            int isInitialized = newRhino.rhino_app.IsInitialized();
            if (isInitialized != 1)
            {
                return false;
            }

            // load scene
            return true;
        }

    }
}
