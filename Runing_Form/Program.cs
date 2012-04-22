using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Web.Mail;

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


            String mailTo = "tamir@ez3d.co";
            String subject = "Rhino Server on Machine= " + Environment.MachineName + " is ready for rendering...";
            Console.WriteLine("Sending mail to " + mailTo);

/*
            // create mail message object
            MailMessage mail = new MailMessage();
            mail.From = mailTo; // put the from address here
            mail.To = mailTo; // put to address here
            mail.Subject = subject; // put subject here
            mail.Body = String.Empty; // put body of email here
            SmtpMail.SmtpServer = "smtp.gmail.com"; // put smtp server you will use here
            // and then send the mail
            SmtpMail.Send(mail);
*/


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            Application.Run(new Form1());
        }
    }
}
