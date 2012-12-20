using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Process_Manager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            UtilsDLL.Dirs.get_all_relevant_dirs();
            Application.Run(new Manager_Form(args[0]));
        }
    }
}
