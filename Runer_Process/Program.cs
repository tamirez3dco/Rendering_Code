using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using Rhino4;
using System.Diagnostics;
using UtilsDLL;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Runer_Process
{

    class Program
    {
        private static Semaphore load_rhino_gate;
        private static Semaphore make_cycle_gate;

        private static UtilsDLL.Rhino.Rhino_Wrapper rhino_wrapper;

        public static Dictionary<String, Object> params_dict;

        static void log(String str)
        {
            int id = (int)params_dict["id"];
            Console.WriteLine((id.ToString() + "): Before rhino gate.WaitOne() : " + DateTime.Now.ToString()));
        }

        static void Main(string[] args)
        {
            UtilsDLL.Dirs.get_all_relevant_dirs();

            int whnd = UtilsDLL.Win32_API.FindWindow(null, "RhinoManager");

            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("args["+i+"]="+args[i]);
            }


            // Decifer all needed arguments from the command line
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var jsonObject = serializer.DeserializeObject(args[0]) as Dictionary<string, object>;
            params_dict = (Dictionary<String, Object>)jsonObject;

            int id = (int)params_dict["id"];
            String scene_fileName = (String)params_dict["scene"];
            String name = (String)params_dict["name"];

            // threading semaphore - named global across all machine
            load_rhino_gate = Semaphore.OpenExisting("load_rhino");
            

            //String full_name = 


            log("Before rhino gate.WaitOne() : " + DateTime.Now.ToString());
            // We load the Rhinos one bye one to make sure what is their PID
            // before loading Rhinos ...
            load_rhino_gate.WaitOne();

            log("After rhino gate.WaitOne() : " + DateTime.Now.ToString());
            log("Before rhino creation : " + DateTime.Now.ToString());

            if (!UtilsDLL.Rhino.start_a_SingleRhino(scene_fileName, true, out rhino_wrapper))
            {
                log("startSingleRhino() failed");
                MessageBox.Show("Basa");
                load_rhino_gate.Release();
                return;
            }
             
            log("): After rhino creation : " + DateTime.Now.ToString());
            // get a list with new Rhino.. that was not there before..

            UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Finshed Rhino");
            //sw.Write("Finished Rhino startup");

            Console.WriteLine(id + "): Before rhino gate.Release() : " + DateTime.Now.ToString());
            load_rhino_gate.Release();
            Console.WriteLine(id + "): After rhino gate.Release() : " + DateTime.Now.ToString());


            make_cycle_gate  = Semaphore.OpenExisting("make_cycle");
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
