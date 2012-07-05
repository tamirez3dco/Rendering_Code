using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using Rhino4;
using System.Diagnostics;

namespace Runer_Process
{
    public class Rhino_Wrapper
    {
        public Rhino5Application rhino_app;
        public int rhino_pid;
        public DateTime creationTime;
        public DateTime killTime;
        public dynamic grasshopper;
    }

    class Program
    {
        private static Semaphore load_rhino_gate;
        private static Semaphore make_cycle_gate;

        private static int id;
        private static Rhino_Wrapper rhino_wrapper;

        private static bool startSingleRhino(String sceneFilePath, bool rhino_visible, out Rhino_Wrapper newRhino)
        {
            newRhino = new Rhino_Wrapper();

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

            return true;
        }

        static void log(String str)
        {
            Console.WriteLine(id + "): Before rhino gate.WaitOne() : " + DateTime.Now.ToString());
        }

        static void Main(string[] args)
        {
            int whnd = UtilsDLL.Win32_API.FindWindow(null, "Form1");

            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("args["+i+"]="+args[i]);
            }


            // Decifer all needed arguments from the command line

            id = int.Parse(args[0]);

            load_rhino_gate = Semaphore.OpenExisting("load_rhino");
            // SQS shit

            log("Before rhino gate.WaitOne() : " + DateTime.Now.ToString());
            // We load the Rhinos one bye one to make sure what is their PID
            // before loading Rhinos ...
            load_rhino_gate.WaitOne();

            log("After rhino gate.WaitOne() : " + DateTime.Now.ToString());
            log("Before rhino creation : " + DateTime.Now.ToString());

            if (!startSingleRhino("", true, out rhino_wrapper))
            {
                log("startSingleRhino() failed");
                load_rhino_gate.Release();
                return;
            }
             
            Console.WriteLine(id + "): After rhino creation : " + DateTime.Now.ToString());
            // get a list with new Rhino.. that was not there before..

            UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Finshed Rhino");
            //sw.Write("Finished Rhino startup");

            Console.WriteLine(id + "): Before rhino gate.Release() : " + DateTime.Now.ToString());
            load_rhino_gate.Release();
            Console.WriteLine(id + "): After rhino gate.Release() : " + DateTime.Now.ToString());


            make_cycle_gate  = new Semaphore(0, 2, "make_cycle");
            while (true)
            {
                Console.WriteLine(id + "): Before cycle gate.Wait one() : " + DateTime.Now.ToString());
                make_cycle_gate.WaitOne();
                Console.WriteLine(id + "): After cycle gate.Wait one() : " + DateTime.Now.ToString());

                // check for message on my Q

                // if msg - 

                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Started cycle");
                //          cycle()
                for (int t1 = 0; t1 < 20; t1++)
                {
                    Console.WriteLine(id + "): Processing image t1=" + t1);
                    Thread.Sleep(150);
                }
                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Finished cycle");


                Console.WriteLine(id + "): Before cycle gate.Release() : " + DateTime.Now.ToString());
                make_cycle_gate.Release();
                Console.WriteLine(id + "): After cycle gate.Release() : " + DateTime.Now.ToString());
            }
        }
    }
}
