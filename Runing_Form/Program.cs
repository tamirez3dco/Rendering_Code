using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net.Mail;
using System.Net;

namespace Runing_Form
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            // redirect console output to parent process;
            // must be before any calls to Console.WriteLine()
            AttachConsole(ATTACH_PARENT_PROCESS);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                Utils.is_amazon_machine = true; // get the CFG dictionary from AWS User_Data
                                                // String itself is read later on on ControlloerForm.startAll()
            }
            else
            {
                Utils.is_amazon_machine = false; // a localHost debug session - get the CFG dictionary from Application param
                Utils.user_Data_String = args[0];
            }
            Application.Run(new Runing_Form());
        }
    }
}
