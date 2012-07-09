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
        private static int id_counter, seconds_timeout;
        private static String error_Q_url, bucket_name;

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

            Fuckups_DB.Open_Connection();
            Fuckups_DB.Clear_DB();


            load_rhino_gate = new Semaphore(0, 1, "load_rhino");
            make_cycle_gate = new Semaphore(0, 2, "make_cycle");

            load_rhino_gate.Release(1);
            make_cycle_gate.Release(2);

            String name = (String)params_dict["name"];
            bucket_name = name + "_Bucket";
            if (!S3_Utils.Make_Sure_Bucket_Exists(bucket_name))
            {
                MessageBox.Show("S3_Utils.Make_Sure_Bucket_Exists(bucket_name="+bucket_name+") failed!!!");
                return;
            }
            seconds_timeout = (int)params_dict["timeout"];
            int mult = (int)params_dict["mult"];
            id_counter = 0;
            foreach (Object scene_obj in (Object[])params_dict["scenes"])
            {
                String scene = (String)scene_obj;
                String half_name = name + '_' + scene;
                String request_Q_url, ready_Q_url;
                if (!make_sure_SQS_Qs_exist(half_name,out request_Q_url,out ready_Q_url, out error_Q_url))
                {
                    MessageBox.Show("!make_sure_SQS_Qs_exist(" + half_name + ") failed!!!");
                    return;
                }

                for (int j = 0; j < mult; j++, id_counter++)
                {
                    Dictionary<String,Object> single_scene_params_dict = new Dictionary<string,object>();
                    single_scene_params_dict["id"] = id_counter;
                    single_scene_params_dict["scene"] = scene + ".3dm";
                    single_scene_params_dict["request_Q_url"] = request_Q_url;
                    single_scene_params_dict["ready_Q_url"] = ready_Q_url;
                    single_scene_params_dict["error_Q_url"] = error_Q_url;
                    single_scene_params_dict["bucket_name"] = bucket_name;
                    single_scene_params_dict["timeout"] = seconds_timeout;
                    single_scene_params_dict["rhino_visible"] = false;
                    
                    Start_New_Runner(single_scene_params_dict);
                }
            }
        }

        private void Start_New_Runner(Dictionary<String, Object> single_scene_params_dict)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
            string jsonString = serializer.Serialize((object)single_scene_params_dict);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Arguments = jsonString.Replace("\"", "\\\"");
            psi.FileName = @"C:\Inetpub\ftproot\Rendering_Code\Runer_Process\bin\Debug\Runer_Process.exe";
            psi.UseShellExecute = true;
            Process p = Process.Start(psi);

            dataGridView1.Rows.Add(
                single_scene_params_dict["id"],
                single_scene_params_dict["scene"],
                "Started",
                String.Empty,
                DateTime.Now.ToString(),
                String.Empty,
                p.Id,
                single_scene_params_dict["request_Q_url"],
                single_scene_params_dict["ready_Q_url"]);
        }

        private static char[] tokenizer = { ' ' };


        private void change_grid_row(int runer_id, String msg)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                int runer_id_from_row = (int)row.Cells[(int)ColumnsIndex.RUNER_ID].Value;
                if (runer_id_from_row == runer_id)
                {
                    if (msg.StartsWith("render_model")) // need to adjust item_id
                    {
                        String[] tokens = msg.Split(tokenizer);
                        String item_id = tokens[2];
                        String modeling_state = tokens[1];
                        String stateToWrite = modeling_state + " model";
                        row.Cells[(int)ColumnsIndex.STATE].Value = stateToWrite;
                        row.Cells[(int)ColumnsIndex.ITEM_ID].Value = item_id;
                    }
                    else if (msg.StartsWith("Finished_Rhino")) // need to adjust Rhino PID
                    {
                        String[] tokens = msg.Split(tokenizer);
                        int rhino_pid = int.Parse(tokens[1]);
                        row.Cells[(int)ColumnsIndex.RHINO_PID].Value = rhino_pid;
                    }
                    else if (msg.StartsWith("ERROR")) // need to kill correct Rhino process + correct runer process
                    {
                        Fuckups_DB.Add_Fuckup((String)row.Cells[(int)ColumnsIndex.ITEM_ID].Value);

                        int rhino_pid = (int)row.Cells[(int)ColumnsIndex.RHINO_PID].Value;
                        int runer_pid = (int)row.Cells[(int)ColumnsIndex.RUNER_PID].Value;
                        // color row to red
                        row.DefaultCellStyle.BackColor = Color.Red;
                        Win32_API.Kill_Process(rhino_pid);
                        Win32_API.Kill_Process(runer_pid);
                        row.Cells[(int)ColumnsIndex.STATE].Value = "killed";
                        // release lock  (was not done by runer)
                        make_cycle_gate.Release(1);

                        // start a new process....
                        Dictionary<String, Object> single_scene_params_dict = new Dictionary<string, object>();
                        single_scene_params_dict["id"] = id_counter++;
                        single_scene_params_dict["scene"] = (String)row.Cells[(int)ColumnsIndex.SCENE].Value;
                        single_scene_params_dict["request_Q_url"] = row.Cells[(int)ColumnsIndex.REQUEST_URL].Value;
                        single_scene_params_dict["ready_Q_url"] = row.Cells[(int)ColumnsIndex.READY_URL].Value;
                        single_scene_params_dict["error_Q_url"] = error_Q_url;
                        single_scene_params_dict["bucket_name"] = bucket_name;
                        single_scene_params_dict["timeout"] = seconds_timeout;
                        single_scene_params_dict["rhino_visible"] = false;

                        Start_New_Runner(single_scene_params_dict);
                    }
                    else if (msg.StartsWith("FUCKUP DELETED"))
                    {
                        row.Cells[(int)ColumnsIndex.STATE].Value = msg;
                    }
                    else
                    {
                        row.Cells[(int)ColumnsIndex.STATE].Value = msg;
                    }
                    row.Cells[(int)ColumnsIndex.LAST_UPDATE].Value = DateTime.Now.ToString();
                    dataGridView1.Refresh();
                    return;
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
                    String msg = mystr.lpData;
                    int runer_id = m.WParam.ToInt32();
                    Console.WriteLine("Got message("+runer_id.ToString()+"):"+msg);
                    change_grid_row(runer_id, msg);
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

        private void check_crashes_timer_Tick(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                String state = (String)row.Cells[(int)ColumnsIndex.STATE].Value;
                if (state.Trim().ToLower() == "starting model")
                {
                    DateTime lastUpdate = DateTime.Parse((String)row.Cells[(int)ColumnsIndex.LAST_UPDATE].Value);
                    TimeSpan diff = DateTime.Now - lastUpdate;
                    if (diff.TotalSeconds > 55)
                    {
                        Fuckups_DB.Add_Fuckup((String)row.Cells[(int)ColumnsIndex.ITEM_ID].Value);
                        int rhino_pid = (int)row.Cells[(int)ColumnsIndex.RHINO_PID].Value;
                        int runer_pid = (int)row.Cells[(int)ColumnsIndex.RUNER_PID].Value;
                        // color row to red
                        row.DefaultCellStyle.BackColor = Color.Red;
                        Win32_API.Kill_Process(rhino_pid);
                        Win32_API.Kill_Process(runer_pid);
                        row.Cells[(int)ColumnsIndex.STATE].Value = "killed";
                        // release lock  (was not done by runer)
                        make_cycle_gate.Release(1);

                        // start a new process....
                        Dictionary<String, Object> single_scene_params_dict = new Dictionary<string, object>();
                        single_scene_params_dict["id"] = id_counter++;
                        single_scene_params_dict["scene"] = (String)row.Cells[(int)ColumnsIndex.SCENE].Value;
                        single_scene_params_dict["request_Q_url"] = row.Cells[(int)ColumnsIndex.REQUEST_URL].Value;
                        single_scene_params_dict["ready_Q_url"] = row.Cells[(int)ColumnsIndex.READY_URL].Value;
                        single_scene_params_dict["error_Q_url"] = error_Q_url;
                        single_scene_params_dict["bucket_name"] = bucket_name;
                        single_scene_params_dict["timeout"] = seconds_timeout;
                        single_scene_params_dict["rhino_visible"] = false;

                        Start_New_Runner(single_scene_params_dict);

                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Fuckups_DB.Open_Connection();
            Fuckups_DB.Add_Fuckup("aaaa");
            //int fuckers = Fuckups_DB.Get_Fuckups("aaaa");
            //Fuckups_DB.Clear_DB();
        }

    }

    public enum ColumnsIndex
    {
        RUNER_ID = 0,
        SCENE = 1,
        STATE = 2,
        ITEM_ID = 3,
        LAST_UPDATE = 4,
        RHINO_PID = 5,
        RUNER_PID = 6,
        REQUEST_URL = 7,
        READY_URL = 8
    }

}
