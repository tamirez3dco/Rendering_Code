using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Pipes;
using System.IO;

namespace Runer_Process
{
    class Program
    {
        private static Semaphore load_rhino_gate;
        private static Semaphore make_cycle_gate;

        private static PipeStream pipeClient;
                
        private static String c;
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("args["+i+"]="+args[i]);
            }
            pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, args[1]);
            StreamWriter sw = new StreamWriter(pipeClient);
            // Decifer all needed arguments from the command line

            c = args[0];

            load_rhino_gate = Semaphore.OpenExisting("load_rhino");
            // SQS shit

            Console.WriteLine(c + "): Before rhino gate.WaitOne() : " + DateTime.Now.ToString());
            // We load the Rhinos one bye one to make sure what is their PID
            // before loading Rhinos ...
            load_rhino_gate.WaitOne();

            Console.WriteLine(c + "): After rhino gate.WaitOne() : " + DateTime.Now.ToString());
            // get list of Rhino4.exe pids before starting a new one

            Console.WriteLine(c + "): Before rhino creation : " + DateTime.Now.ToString());
            // Start a new Rhino
            for (int t1 = 0; t1 < 20; t1++)
            {
                Console.WriteLine(c + "): Creating rhino t1=" + t1);
                Thread.Sleep(250);
            }
                

            Console.WriteLine(c + "): After rhino creation : " + DateTime.Now.ToString());
            // get a list with new Rhino.. that was not there before..


            sw.Write("Finished Rhino startup");

            Console.WriteLine(c + "): Before rhino gate.Release() : " + DateTime.Now.ToString());
            load_rhino_gate.Release();
            Console.WriteLine(c + "): After rhino gate.Release() : " + DateTime.Now.ToString());


            make_cycle_gate  = new Semaphore(0, 2, "make_cycle");
            while (true)
            {
                Console.WriteLine(c + "): Before cycle gate.Wait one() : " + DateTime.Now.ToString());
                make_cycle_gate.WaitOne();
                Console.WriteLine(c + "): After cycle gate.Wait one() : " + DateTime.Now.ToString());

                // check for message on my Q

                // if msg - 

                //          inform manager on start
                //          cycle()
                //          inform manager on end
                for (int t1 = 0; t1 < 20; t1++)
                {
                    Console.WriteLine(c + "): Processing image t1=" + t1);
                    Thread.Sleep(150);
                }


                Console.WriteLine(c + "): Before cycle gate.Release() : " + DateTime.Now.ToString());
                make_cycle_gate.Release();
                Console.WriteLine(c + "): After cycle gate.Release() : " + DateTime.Now.ToString());
            }
        }
    }
}
