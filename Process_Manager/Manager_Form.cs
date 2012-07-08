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
using UtilsDLL;
using System.Web.Script.Serialization;


namespace Process_Manager
{
    public partial class Manager_Form : Form
    {
        private static Semaphore load_rhino_gate;
        private static Semaphore make_cycle_gate;

        public static Dictionary<String, Object> params_dict;

        public Manager_Form(String scene_params_json)
        {
            InitializeComponent();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var jsonObject = serializer.DeserializeObject(scene_params_json) as Dictionary<string, object>;
            params_dict = (Dictionary<String, Object>)jsonObject;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            UtilsDLL.Dirs.get_all_relevant_dirs();
/*
            bool b; String s;
            UtilsDLL.S3_Utils.Find_Bucket("tamir_Bucket", out b, out s);
*/

            load_rhino_gate = new Semaphore(0, 1, "load_rhino");
            make_cycle_gate = new Semaphore(0, 2, "make_cycle");

            load_rhino_gate.Release(1);
            make_cycle_gate.Release(2);

            String name = (String)params_dict["name"];
            String bucket_name = name + "_Bucket";
            if (!S3_Utils.Make_Sure_Bucket_Exists(bucket_name))
            {
                MessageBox.Show("S3_Utils.Make_Sure_Bucket_Exists(bucket_name="+bucket_name+") failed!!!");
                return;
            }
            int seconds_timeout = (int)params_dict["timeout"];
            int mult = (int)params_dict["mult"];
            int id_counter = 0;
            foreach (Object scene_obj in (Object[])params_dict["scenes"])
            {
                String scene = (String)scene_obj;
                String half_name = name + '_' + scene;
                String request_Q_url, ready_Q_url, error_Q_url;
                if (!make_sure_SQS_Qs_exist(half_name,out request_Q_url,out ready_Q_url, out error_Q_url))
                {
                    MessageBox.Show("!make_sure_SQS_Qs_exist(" + half_name + ") failed!!!");
                    return;
                }

                for (int j = 0; j < mult; j++, id_counter++)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    Dictionary<String,Object> single_scene_params_dict = new Dictionary<string,object>();
                    single_scene_params_dict["id"] = id_counter;
                    single_scene_params_dict["scene"] = scene + ".3dm";
                    single_scene_params_dict["request_Q_url"] = request_Q_url;
                    single_scene_params_dict["ready_Q_url"] = ready_Q_url;
                    single_scene_params_dict["error_Q_url"] = error_Q_url;
                    single_scene_params_dict["bucket_name"] = bucket_name;
                    single_scene_params_dict["timeout"] = seconds_timeout;


                    JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                    string jsonString = serializer.Serialize((object)single_scene_params_dict);


                    psi.Arguments = jsonString.Replace("\"","\\\"");
                    psi.FileName = @"C:\Amit\Rendering_Code\Runer_Process\bin\Debug\Runer_Process.exe";
                    psi.UseShellExecute = true;
                    Process p = Process.Start(psi);
                }
            }
        }

        private bool make_sure_SQS_Qs_exist(string half_name, out String request_Q_url, out String ready_Q_url, out String error_Q_url)
        {
            request_Q_url = String.Empty;
            ready_Q_url = String.Empty;
            error_Q_url = String.Empty;
            String sqs_request_q_name = half_name + '_' + "request";
            if (!SQS_Utils.Make_sure_Q_exists(sqs_request_q_name, out request_Q_url)) return false;
            String sqs_ready_q_name = half_name + '_' + "ready";
            if (!SQS_Utils.Make_sure_Q_exists(sqs_ready_q_name, out ready_Q_url)) return false;
            String sqs_error_q_name = "GENERAL_ERROR";
            if (!SQS_Utils.Make_sure_Q_exists(sqs_error_q_name, out error_Q_url)) return false;

            return true;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                    
                case Win32_API.WM_COPYDATA:
                    COPYDATASTRUCT mystr = new COPYDATASTRUCT();
                    Type mytype = mystr.GetType();
                    mystr = (COPYDATASTRUCT)m.GetLParam(mytype);
                    Console.WriteLine("Got message("+m.WParam.ToInt32().ToString()+"):"+mystr.lpData);
                    break;
            }
            base.WndProc(ref m);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "taskkill";
            psi.Arguments = "/F /IM Runer_Process.exe";
            Process.Start(psi);

            psi.FileName = "taskkill";
            psi.Arguments = "/F /IM Rhino4.exe";
            Process.Start(psi);
        
        
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }

}
