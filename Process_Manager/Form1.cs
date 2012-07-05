using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;


namespace Process_Manager
{
    public partial class Form1 : Form
    {
        private static Semaphore load_rhino_gate;
        private static Semaphore make_cycle_gate;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            load_rhino_gate = new Semaphore(0, 1, "load_rhino");
            make_cycle_gate = new Semaphore(0, 2, "make_cycle");

            load_rhino_gate.Release(1);
            make_cycle_gate.Release(2);


            for (int i = 0; i < 5; i++)
            {
                AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.Arguments = i.ToString() + " " + pipeServer.GetClientHandleAsString();
                psi.FileName = @"C:\Amit\Rendering_Code\Runer_Process\bin\Debug\Runer_Process.exe";
                psi.UseShellExecute = true;

                Process p = Process.Start(psi);

                MyStreamReader_Thread localListener = new MyStreamReader_Thread();
                localListener.id = i;
                localListener.sr = new StreamReader(pipeServer);
                ThreadStart ts = new ThreadStart(localListener.listener);
                new Thread(ts).Start();

                pipeServer.DisposeLocalCopyOfClientHandle();
            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "taskkill";
            psi.Arguments = "/F /IM Runer_Process.exe";
            Process.Start(psi);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }
    }

    public class MyStreamReader_Thread
    {
        public int id;
        public StreamReader sr;

        public void listener()
        {
            String temp;
            while ((temp = sr.ReadLine()) != null)
            {
                Console.WriteLine("["+id+"] " + temp);
            }
        }
    }
}
