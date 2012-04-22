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

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;

namespace Runing_Form
{
    public partial class Form1 : Form
    {


        List<GHR> ghrs;

        int num_of_rhinos = -1;
        bool rhino_visible = false;
        String sceneFileName;

        public static String scenes_DirPath = null;
        public static String GH_DirPath = null;
        public static String images_DirPath = null;
        public static String my_ip = null;

        // debug
        bool lookForIp_AmazonStyle;

        public Form1()
        {
            InitializeComponent();
        }
        private bool Get_My_IP(out String res)
        {
            res = null;

            string sURL = "http://169.254.169.254/latest/meta-data/public-ipv4";

            try
            {
                WebRequest wrGETURL;
                wrGETURL = WebRequest.Create(sURL);
                WebProxy myProxy = new WebProxy("myproxy", 80);
                myProxy.BypassProxyOnLocal = true;

                Stream objStream;
                objStream = wrGETURL.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(objStream);
                res = sr.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine("Excpetion in Get_My_IP(). = " + e.Message);
                return false;
            }

            return true;
        }

        private bool read_params_from_cfg_file()
        {
            if (!Utils.Read_Cfg_File())
            {
                Console.WriteLine("ERROR !!! - Utils.Read_Cfg_File() faild!!!");
            }

            String tempParamName;
            tempParamName = "num_of_rhino_instances";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else this.num_of_rhinos = int.Parse(Utils.CFG[tempParamName]);

            tempParamName = "visible_rhino";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else this.rhino_visible = bool.Parse(Utils.CFG[tempParamName]);

            tempParamName = "scene";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else this.sceneFileName = Utils.CFG[tempParamName];

            tempParamName = "is_amazon_ip";
            if (!Utils.CFG.ContainsKey(tempParamName))
            {
                Console.WriteLine("param " + tempParamName + " is not found in ez3d.config");
                return false;
            }
            else this.lookForIp_AmazonStyle = bool.Parse(Utils.CFG[tempParamName]);

            return true;
        }

        private bool start_all()
        {

            Console.WriteLine("Form constructed at time : " + DateTime.Now);
            if (!read_params_from_cfg_file())
            {
                Console.WriteLine("ERROR - read_params_from_cfg_file() failed!!");
                return false;
            }

            if (lookForIp_AmazonStyle)
            {
                if (!Get_My_IP(out my_ip))
                {
                    Console.WriteLine("ERROR !!! - get_relevant_dirs() failed!!!");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Flagged that machine uses a dummy ip=localhost as in a debug mode on a local machine (Not AWS)");
                my_ip = "localhost";
            }

            if (!get_relevant_dirs())
            {
                Console.WriteLine("ERROR !!! - get_relevant_dirs() failed!!!");
                return false;
            }

/*
            if (!Get_Queues_URLs())
            {
                Console.WriteLine("Get_Queues_URLs() failed!!!");
                return false;
            }
*/
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

            return true;
        }

        private bool get_Scenes_Dir(out String scenesDirPath)
        {
            scenesDirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            scenesDirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "R_S_Data" + Path.DirectorySeparatorChar + "Scene";
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
            GH_DirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar +"R_S_Data" + Path.DirectorySeparatorChar + "GH_Def_files";
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
            image_DirPath = dir.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "tempImageFiles";
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
        private void Form1_Load(object sender, EventArgs e)
        {

            if (!start_all())
            {
                Console.WriteLine("ERROR !!! - start_all() failed!!!");
                return;
            }
            Console.WriteLine("start_all() finished succesfully");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var newVsibility = !this.rhino_visible;
            foreach (GHR ghr in ghrs)
            {
                ghr.rhino.Visible = newVsibility ? 1 : 0;
            }
            this.rhino_visible = newVsibility;
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
