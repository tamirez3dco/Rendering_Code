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
using System.Security.AccessControl;
using System.IO;
using System.Security.Principal;
using Rhino4;


using System.Net;
using System.Web.Script.Serialization;

namespace Runing_Form
{
    public partial class Runing_Form : Form
    {


        List<GHR> ghrs;

        public static int num_of_rhinos = -1;
        public static bool rhino_visible = false;
        String sceneFileName;

        public static String scenes_DirPath = null;
        public static String GH_DirPath = null;
        public static String images_DirPath = null;


        public Runing_Form()
        {
            InitializeComponent();
        }

        private bool read_params_from_user_data()
        {
            if (!Utils.Get_Launch_Specific_Data())
            {
                Console.WriteLine("ERROR !!! - Utils.Get_Launch_Specific_Data() faild!!!");
            }

            String tempParamName;
            tempParamName = "num_of_rhino_instances";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else num_of_rhinos = (int)Utils.CFG[tempParamName];

            tempParamName = "visible_rhino";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else Runing_Form.rhino_visible = (bool)Utils.CFG[tempParamName];

            tempParamName = "scene";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else this.sceneFileName = (String)Utils.CFG[tempParamName];

/*
            tempParamName = "is_amazon_ip";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else this.is_amazon_machine = (bool)Utils.CFG[tempParamName];
*/
            return true;
        }

        private bool start_all()
        {
            Console.WriteLine("Form constructed at time : " + DateTime.Now);
            if (!read_params_from_user_data())
            {
                Console.WriteLine("ERROR - read_params_from_cfg_file() failed!!");
                return false;
            }

            if (Utils.is_amazon_machine)
            {
                if (!Utils.Get_My_IP_AND_DNS())
                {
                    Console.WriteLine("ERROR !!! - Get_My_IP_AND_DNS() failed!!!");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Flagged that machine uses a dummy ip=localhost as in a debug mode on a local machine (Not AWS)");
                Utils.my_ip = "localhost";
            }

            
            if (!SQS.Initialize_SQS_stuff())
            {
                Console.WriteLine("ERROR !!!- Initialize_SQS_stuff() failed!!");
                return false;
            }

            if (!S3.Initialize_S3_stuff())
            {
                Console.WriteLine("ERROR !!!- Initialize_S3_stuff() failed!!");
                return false;
            }

            if (!Utils.Refresh_Rhino_GH_Data_From_Github())
            {
                Console.WriteLine("ERROR !!!- Refresh_Rhino_GH_Data_From_Github() failed!!");
                return false;
            }
            

            if (!get_relevant_dirs())
            {
                Console.WriteLine("ERROR !!! - get_relevant_dirs() failed!!!");
                return false;
            }

            // kill all current Rhino4.exe processes
            Process[] procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Killing " + procs.Length + " previous Rhino processes");
            foreach (Process p in procs) { p.Kill(); }
            Thread.Sleep(1000);
            procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine(procs.Length + " previous Rhino processes remaind alive");

            ghrs = new List<GHR>();
            for (int i = 0; i < num_of_rhinos; i++)
            {
                Console.WriteLine("Starting Rhino # " + i + " at " + DateTime.Now);
                Rhino5Application rhino = new Rhino5Application();
                rhino.Visible = 1;
                if (rhino == null)
                {
                    Console.WriteLine("(i=" + i + ") rhino == null");
                    return false;
                }
                for (int tries = 0; tries < 200; tries++)
                {
                    if (rhino.IsInitialized() == 1)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                Thread.Sleep(1000);

                String sceneFilePath = scenes_DirPath + Path.DirectorySeparatorChar + sceneFileName;
                String replicateFilePath = scenes_DirPath + Path.DirectorySeparatorChar + "rep_" + i + ".3dm";
                File.Copy(sceneFilePath, replicateFilePath, true);


                Console.WriteLine("Loading scene Rhino # " + i + " at " + DateTime.Now);
                String openCommand = "-Open " + replicateFilePath;
                int openCommandRes = rhino.RunScript(openCommand, 1);

                int isInitialized = rhino.IsInitialized();
                if (isInitialized != 1)
                {
                    Console.WriteLine("ERROR!!: (i=" + i + ") (" + isInitialized + "==isInitialized != 1)");
                    return false;
                }
                Console.WriteLine("Starting Grasshopper # " + i + " at " + DateTime.Now);
                rhino.RunScript("_Grasshopper", 0);
                Thread.Sleep(2000);
                dynamic grasshopper = rhino.GetPlugInObject("b45a29b1-4343-4035-989e-044e8580d9cf", "00000000-0000-0000-0000-000000000000") as dynamic;


                if (grasshopper == null)
                {
                    Console.WriteLine("ERROR!!: (i=" + i + ") (grasshopper == null)");
                    return false;
                }

                GHR ghr = new GHR(i, rhino, grasshopper);
                ghrs.Add(ghr);


                rhino.Visible = rhino_visible ? 1 : 0;


                Thread thread = new Thread(new ThreadStart(ghr.new_runner));
                thread.Start();

                Console.WriteLine("Finished instance #" + i + " at " + DateTime.Now);

            }


            numOfInstances_textBox.BackColor = Color.Green;
            numOfInstances_textBox.Text = num_of_rhinos.ToString();


            // Send "Server Ready" msg
            if (!SQS.Send_Server_Ready_Message())
            {
                Console.WriteLine("ERROR - Send_Server_Ready_Message() failed!!!");
                return false;
            }

            return true;
        }

        private bool get_Scenes_Dir(out String scenesDirPath)
        {
            scenesDirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            scenesDirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Rendering_Data" + Path.DirectorySeparatorChar + "Scene";
            if (!Directory.Exists(scenesDirPath))
            {
                Console.WriteLine("ERROR!!! - Could not find ScenesDir=" + scenesDirPath);
                return false;

            }
            Console.WriteLine("ScenesDir = " + scenesDirPath);
            return true;
        }

        private bool get_grasshopper_files_Dir(out String GH_DirPath)
        {
            GH_DirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            GH_DirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Rendering_Data" + Path.DirectorySeparatorChar + "GH_Def_files";
            if (!Directory.Exists(GH_DirPath))
            {
                Console.WriteLine("ERROR!!! - Could not find GH_DirPathr=" + GH_DirPath);
                return false;

            }
            Console.WriteLine("GH_DirPath = " + GH_DirPath);
            return true;
        }

        private bool get_tempImages_files_Dir(out String image_DirPath)
        {
            image_DirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            image_DirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "tempImageFiles";
            if (!Directory.Exists(image_DirPath))
            {
                bool creationSuccess = false;
                try
                {
                    DirectoryInfo createDif = Directory.CreateDirectory(image_DirPath);
                    creationSuccess = createDif.Exists;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR !!! - Exception in get_tempImages_files_Dir()");
                    Console.WriteLine(e.Message);
                    creationSuccess = false;
                }
                if (!creationSuccess)
                {
                    Console.WriteLine("ERROR!!! - Could not find or create image_DirPath=" + image_DirPath);
                    return false;
                }
            }
            Console.WriteLine("image_DirPath = " + image_DirPath);
            return true;
        }


        private bool get_relevant_dirs()
        {
            if (!get_Scenes_Dir(out scenes_DirPath)) return false;
            if (!get_grasshopper_files_Dir(out GH_DirPath)) return false;
            if (!get_tempImages_files_Dir(out images_DirPath)) return false;
            return true;
        }
        private void Runing_Form_Load(object sender, EventArgs e)
        {

            if (!start_all())
            {
                Console.WriteLine("ERROR !!! - start_all() failed!!!");
                return;
            }
            Console.WriteLine("start_all() finished succesfully");
        }

        private bool Send_Email(String msg, String recepient, String sender)
        {
            return true;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            var newVsibility = !Runing_Form.rhino_visible;
            foreach (GHR ghr in ghrs)
            {
                ghr.rhino.Visible = newVsibility ? 1 : 0;
            }
            Runing_Form.rhino_visible = newVsibility;
        }

        private void restartRhinosButton_Click(object sender, EventArgs e)
        {
            numOfInstances_textBox.BackColor = Color.Red;
            if (!start_all())
            {
                Console.WriteLine("start_all() failed!!! Halting");
                return;
            }
            numOfInstances_textBox.BackColor = Color.Green;
        }

   
        //[{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":135,"tamir_curveLength":0,"tamir_PipeRadius":0.25,"bake":"Pipe"},{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":136,"tamir_curveLength":0.2,"tamir_PipeRadius":0.5,"bake":"Pipe"},{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":135,"tamir_curveLength":1,"tamir_PipeRadius":1,"bake":"Pipe"}]
        //[{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":135,"tamir_curveLength":0,"tamir_PipeRadius":0.25,"bake":"Pipe"}]
        //[{"gh_file":"brace1.gh","item_id":139,"NumCircles":0.5,"bake":"Bracelet"}]
   

     

     
  
    }
}
