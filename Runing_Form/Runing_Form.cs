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
        public static String PythonScripts_DirPath_Git = null;


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


            // Note that the condition on this one is different. 
            // It is here just to allow debugging on AWS machines
            tempParamName = "is_amazon_machine";
            if (Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " WAS found !!!. Probably a DEBUG run...");
                Utils.is_amazon_machine = (bool)Utils.CFG[tempParamName];
            }

            return true;
        }

        private bool start_all()
        {
            Console.WriteLine("Form constructed at time : " + DateTime.Now);
            // kill all current Rhino4.exe processes
            Process[] procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Killing " + procs.Length + " previous Rhino processes");
            foreach (Process p in procs) { p.Kill(); }
            Thread.Sleep(1000);
            procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine(procs.Length + " previous Rhino processes remaind alive");

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

            if (!get_relevant_dirs())
            {
                Console.WriteLine("ERROR !!! - get_relevant_dirs() failed!!!");
                return false;
            }


            if (!Utils.Refresh_Rhino_GH_Data_From_Github())
            {
                Console.WriteLine("ERROR !!!- Refresh_Rhino_GH_Data_From_Github() failed!!");
                return false;
            }






            ghrs = new List<GHR>();
            for (int i = 0; i < num_of_rhinos; i++)
            {
                Rhino_Wrapper rhino;
                if (!startSingleRhino(out rhino))
                {
                    Console.WriteLine("startSingleRhino failed!!!");
                    return false;
                }
                String sceneFilePath = scenes_DirPath + Path.DirectorySeparatorChar + sceneFileName;
/*
                String replicateFilePath = scenes_DirPath + Path.DirectorySeparatorChar + "rep_" + i + "_"+sceneFileName;
                File.Copy(sceneFilePath, replicateFilePath, true);
*/

                Console.WriteLine("Loading scene Rhino # " + i + " at " + DateTime.Now);
                String openCommand = "-Open " + sceneFilePath;
                int openCommandRes = rhino.rhino_app.RunScript(openCommand, 1);


                GHR ghr = new GHR(i, rhino);
                ghr.current_Rhino_File = sceneFileName;
                ghrs.Add(ghr);

                String editPythonCommand = "EditPythonScript";
                rhino.rhino_app.RunScript(editPythonCommand, 0);
                Thread.Sleep(2000);

                rhino.rhino_app.Visible = rhino_visible ? 1 : 0;


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

            Utils.lastMsg_Time = DateTime.Now;
            checkSpareRhinos_Timer.Enabled = true;

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

        private bool get_pythonscripts_files_Dir(out String PY_DirPath)
        {
            PY_DirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            PY_DirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Rendering_Data" + Path.DirectorySeparatorChar + "PythonScripts";
            if (!Directory.Exists(PY_DirPath))
            {
                Console.WriteLine("ERROR!!! - Could not find PY_DirPath=" + PY_DirPath);
                return false;

            }
            Console.WriteLine("PY_DirPath = " + PY_DirPath);
            return true;
        }

        public static bool get_tempImages_files_Dir(out String image_DirPath)
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
            if (!get_pythonscripts_files_Dir(out PythonScripts_DirPath_Git)) return false;

            GHR.Python_Scripts_Actual_Folder_Path = @"C:\Users\" + System.Environment.UserName + @"\AppData\Roaming\McNeel\Rhinoceros\5.0\Plug-ins\PythonPlugins\quest {4aa421bc-1d5d-4d9e-9e48-91bf91516ffa}\dev";
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
                ghr.rhino_wrapper.rhino_app.Visible = newVsibility ? 1 : 0;
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


        private bool startSingleRhino(out Rhino_Wrapper newRhino)
        {
            newRhino = new Rhino_Wrapper();
            lock (GHR.locker)
            {
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

        private void checkSpareRhinos_Timer_Tick(object sender, EventArgs e)
        {
            checkSpareRhinos_Timer.Enabled = false;
            if (GHR.spareRhinos.Count >= 2 * num_of_rhinos)
            {
                checkSpareRhinos_Timer.Enabled = true;
                return;
            }

            bool cond_1 = (DateTime.Now - Utils.lastMsg_Time).TotalSeconds > 5;
            bool cond_2 = GHR.spareRhinos.Count < Math.Min(2, num_of_rhinos);
            if (cond_1 || cond_2)
            {
                Console.WriteLine("checkSpareRhinos_Timer_Tick GHR.spareRhinos.Count=" + GHR.spareRhinos.Count + " opening new Rhino");
                Rhino_Wrapper newRhino;
                if (!startSingleRhino(out newRhino))
                {
                    Console.WriteLine("startSingleRhino() failed!!! at " + DateTime.Now);
                    //checkSpareRhinos_Timer.Enabled = true;
                    return;
                }
                GHR.pushRhinoIntoQueue(newRhino);
            }
            else
            {
                //Console.WriteLine("checkSpareRhinos_Timer_Tick GHR.spareRhinos.Count=" + GHR.spareRhinos.Count +" nothing to do");
            }
            checkSpareRhinos_Timer.Enabled = true;

        }


        //[{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":135,"tamir_curveLength":0,"tamir_PipeRadius":0.25,"bake":"Pipe"},{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":136,"tamir_curveLength":0.2,"tamir_PipeRadius":0.5,"bake":"Pipe"},{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":135,"tamir_curveLength":1,"tamir_PipeRadius":1,"bake":"Pipe"}]
        //[{"gh_file":"20091109_ghx060019_Surfaces_Introduction.ghx","item_id":135,"tamir_curveLength":0,"tamir_PipeRadius":0.25,"bake":"Pipe"}]
        //[{"gh_file":"brace1.gh","item_id":139,"NumCircles":0.5,"bake":"Bracelet"}]






    }
}
