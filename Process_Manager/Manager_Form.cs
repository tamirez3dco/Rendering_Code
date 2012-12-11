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
        private static String error_Q_url, bucket_name, stl_bucket_name;
        private static bool rhino_visible = false;
        private static bool skip_empty_check = false;
        private static bool stopOnError = false;
        private static String ghx_bucket_name;
        private static bool disable_low_priority = false;

        public Manager_Form(String scene_params_json)
        {
            InitializeComponent();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var temp_Json = serializer.DeserializeObject(scene_params_json) as Dictionary<string, object>;
            Dictionary<String, Object> temp_params = (Dictionary<String, Object>)temp_Json;
            bool overide_aws_userdata = false;
            if (temp_params.ContainsKey("overide_aws_userdata"))
            {
                overide_aws_userdata = (bool)temp_params["overide_aws_userdata"];
            }

            String jsonStr = String.Empty;
            bool use_cfg_str_from_main = false;
            if (overide_aws_userdata) use_cfg_str_from_main = true;
            else
            {
                if (!UtilsDLL.AWS_Utils.Get_Launch_Specific_Data(out jsonStr))
                {
                    Console.WriteLine("Probably not a AWS machine - could not find userData");
                    use_cfg_str_from_main = true;
                }
                if (String.IsNullOrEmpty(jsonStr)) use_cfg_str_from_main = true;
            }
            if (use_cfg_str_from_main) jsonStr = scene_params_json;

            var jsonObject = serializer.DeserializeObject(jsonStr) as Dictionary<string, object>;
            params_dict = (Dictionary<String, Object>)jsonObject;
            
        }

        private bool startAll()
        {
            try
            {
                UtilsDLL.Dirs.get_all_relevant_dirs();

                killAll();

                if (params_dict.ContainsKey("activate_monitor"))
                {
                    Boolean should_activate = (Boolean)params_dict["activate_monitor"];
                    if (should_activate)
                    {
                        Process[] monitor = Process.GetProcessesByName("ServerMonitor");
                        if (monitor.Length == 0)
                        {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = @"C:\Inetpub\ftproot\Rendering_Code\ServerMonitor\bin\Debug\ServerMonitor.exe";
                            psi.UseShellExecute = true;
                            Process p = Process.Start(psi);
                        }
                    }
                }


                bool refresh_rhino_data = true;
                if (params_dict.ContainsKey("refresh_rhino_data")) refresh_rhino_data = (bool)params_dict["refresh_rhino_data"];
                if (refresh_rhino_data)
                {
                    Dirs.Refresh_Rhino_GH_Data_From_Github();
                }
                if (params_dict.ContainsKey("skip_empty_check")) skip_empty_check = (bool)params_dict["skip_empty_check"];
                if (!skip_empty_check)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = @"C:\Inetpub\ftproot\Rendering_Code\EmptyImagesConstructor\bin\Debug\EmptyImagesConstructor.exe";
                    String temp = (new JavaScriptSerializer()).Serialize((object[])params_dict["scenes"]);
                    psi.Arguments = temp.Replace("\"", "\\\"");
                    psi.UseShellExecute = true;
                    Process p = Process.Start(psi);
                    p.WaitForExit();
                }


                killAll();

                Fuckups_DB.Open_Connection();
                Fuckups_DB.Clear_DB();


                int mult = (int)params_dict["mult"];


                if (params_dict.ContainsKey("disable_low_priority"))
                {
                    disable_low_priority = (bool)params_dict["disable_low_priority"];
                }
                
                if (params_dict.ContainsKey("stopOnERR"))
                {
                    stopOnError = (bool)params_dict["stopOnERR"];
                }

                load_rhino_gate = new Semaphore(0, 1, "load_rhino");
                make_cycle_gate = new Semaphore(0, mult, "make_cycle");

                load_rhino_gate.Release(1);
                make_cycle_gate.Release(mult);

                String name = (String)params_dict["name"];
                bucket_name = name + "_Bucket";

                if (!S3_Utils.Make_Sure_Bucket_Exists(bucket_name))
                {
                    MessageBox.Show("S3_Utils.Make_Sure_Bucket_Exists(bucket_name=" + bucket_name + ") failed!!!");
                    return false;
                }
                stl_bucket_name = name + "_stl_Bucket";
                if (!S3_Utils.Make_Sure_Bucket_Exists(stl_bucket_name))
                {
                    MessageBox.Show("S3_Utils.Make_Sure_Bucket_Exists(stl_bucket_name=" + stl_bucket_name + ") failed!!!");
                    return false;
                }
                //ghx_bucket_name = name + "_ghx_Bucket";
                ghx_bucket_name = "ez3d_media";
                if (!S3_Utils.Make_Sure_Bucket_Exists("ghx_bucket_name"))
                {
                    MessageBox.Show("S3_Utils.Make_Sure_Bucket_Exists(ghx_bucket_name=" + ghx_bucket_name + ") failed!!!");
                    return false;
                }

                seconds_timeout = (int)params_dict["timeout"];
                rhino_visible = (bool)(params_dict["rhino_visible"]);
                id_counter = 0;
                foreach (Object scene_obj in (Object[])params_dict["scenes"])
                {
                    String scene = (String)scene_obj;
                    String request_Q_url, ready_Q_url, requests_lowprioirty_Q_url;
                    if (!make_sure_SQS_Qs_exist(name, scene, out request_Q_url, out requests_lowprioirty_Q_url, out ready_Q_url, out error_Q_url))
                    {
                        MessageBox.Show("!make_sure_SQS_Qs_exist(" + name + "," + scene + ") failed!!!");
                        return false;
                    }


                    for (int j = 0; j < mult; j++, id_counter++)
                    {
                        Dictionary<String, Object> single_scene_params_dict = new Dictionary<string, object>();
                        single_scene_params_dict["id"] = id_counter;
                        single_scene_params_dict["scene"] = scene;
                        single_scene_params_dict["request_Q_url"] = request_Q_url;
                        single_scene_params_dict["request_lowpriority_Q_url"] = requests_lowprioirty_Q_url;
                        single_scene_params_dict["ready_Q_url"] = ready_Q_url;
                        single_scene_params_dict["error_Q_url"] = error_Q_url;
                        single_scene_params_dict["bucket_name"] = bucket_name;
                        single_scene_params_dict["stl_bucket_name"] = stl_bucket_name;
                        single_scene_params_dict["ghx_bucket_name"] = ghx_bucket_name;

                        single_scene_params_dict["timeout"] = seconds_timeout;
                        single_scene_params_dict["rhino_visible"] = rhino_visible;
                        single_scene_params_dict["skip_empty_check"] = skip_empty_check;
                        single_scene_params_dict["disable_low_priority"] = disable_low_priority;


                        Start_New_Runner(single_scene_params_dict);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
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
                String.Empty,
                DateTime.Now.ToString(),
                String.Empty,
                p.Id,
                single_scene_params_dict["request_Q_url"],
                single_scene_params_dict["request_lowpriority_Q_url"],
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

                        String entireJSONReq = String.Empty;
                        for (int i = 3; i < tokens.Length; i++)
                        {
                            entireJSONReq += tokens[i] + " ";
                        }

                        if (modeling_state == "starting")
                        {
                            row.Cells[(int)ColumnsIndex.START_CYCLE_TIME].Value = DateTime.Now.ToString();
                            row.Cells[(int)ColumnsIndex.JSON_REQUEST].Value = entireJSONReq;
                        }
                        else // "finished"
                        {
                            DateTime startCycleTime = DateTime.Parse((String)(row.Cells[(int)ColumnsIndex.START_CYCLE_TIME].Value));
                            TimeSpan cycleDuration = DateTime.Now - startCycleTime;
                            row.Cells[(int)ColumnsIndex.LAST_DURATION].Value = String.Format("{0:0.00}", cycleDuration.TotalSeconds);
                        }
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
                        if (stopOnError)
                        {
                            MessageBox.Show("ERROR");
                        }
                        

                        Fuckups_DB.Add_Fuckup((String)row.Cells[(int)ColumnsIndex.ITEM_ID].Value);

                        row.Cells[(int)ColumnsIndex.ERROR_LINE].Value = msg;
                        
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
                        single_scene_params_dict["request_lowpriority_Q_url"] = row.Cells[(int)ColumnsIndex.REQUEST_LOWPRIORITY_URL].Value;

                        single_scene_params_dict["ready_Q_url"] = row.Cells[(int)ColumnsIndex.READY_URL].Value;
                        single_scene_params_dict["error_Q_url"] = error_Q_url;
                        single_scene_params_dict["bucket_name"] = bucket_name;
                        single_scene_params_dict["stl_bucket_name"] = stl_bucket_name;
                        single_scene_params_dict["ghx_bucket_name"] = ghx_bucket_name;

                        single_scene_params_dict["timeout"] = seconds_timeout;
                        single_scene_params_dict["rhino_visible"] = rhino_visible;
                        single_scene_params_dict["skip_empty_check"] = skip_empty_check;
                        single_scene_params_dict["disable_low_priority"] = disable_low_priority;
                        

                        Start_New_Runner(single_scene_params_dict);
                         
                    }
                    else //("FUCKUP DELETED") or other)
                    {
                        row.Cells[(int)ColumnsIndex.STATE].Value = msg;
                    }

                    if (msg.StartsWith("DELAYER_ON"))
                    {
                        row.DefaultCellStyle.BackColor = Color.Yellow;
                    }
                    if (msg.StartsWith("DELAYER_OFF"))
                    {
                        row.DefaultCellStyle.BackColor = Color.White;
                    }

                    row.Cells[(int)ColumnsIndex.LAST_UPDATE].Value = DateTime.Now.ToString();
                    dataGridView1.Refresh();
                    return;
                }
            }
        }


        private bool make_sure_SQS_Qs_exist(string name, string scene, out String request_Q_url, out String request_lowpriority_Q_url, out String ready_Q_url, out String error_Q_url)
        {
            request_Q_url = String.Empty;
            ready_Q_url = String.Empty;
            error_Q_url = String.Empty;
            request_lowpriority_Q_url = String.Empty;
            String sqs_request_q_name = name + '_' + scene + '_' + "request";
            if (!SQS_Utils.Make_sure_Q_exists(sqs_request_q_name, out request_Q_url)) return false;
            String sqs_requests_low_priority_q_name = name + "_lowpriority_" + scene + '_' + "request";
            if (!SQS_Utils.Make_sure_Q_exists(sqs_requests_low_priority_q_name, out request_lowpriority_Q_url)) return false;
            String sqs_ready_q_name = name + '_' + "ready";
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
                    Console.WriteLine("Got message(" + runer_id.ToString() + "):" + msg);
                    change_grid_row(runer_id, msg);
                    break;
            }
            base.WndProc(ref m);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            killAll();

        }


        public void killAll()
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
            startAll();
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
                    if (diff.TotalSeconds > seconds_timeout)
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
                        try
                        {
                            make_cycle_gate.Release(1);
                        }
                        catch (Exception e2)
                        {
                            Console.WriteLine("Exception e2=" + e2.Message);
                        }
                        

                        // start a new process....
                        Dictionary<String, Object> single_scene_params_dict = new Dictionary<string, object>();
                        single_scene_params_dict["id"] = id_counter++;
                        single_scene_params_dict["scene"] = (String)row.Cells[(int)ColumnsIndex.SCENE].Value;
                        single_scene_params_dict["request_Q_url"] = row.Cells[(int)ColumnsIndex.REQUEST_URL].Value;
                        single_scene_params_dict["ready_Q_url"] = row.Cells[(int)ColumnsIndex.READY_URL].Value;
                        single_scene_params_dict["request_lowpriority_Q_url"] = row.Cells[(int)ColumnsIndex.REQUEST_LOWPRIORITY_URL].Value;
                        single_scene_params_dict["error_Q_url"] = error_Q_url;
                        single_scene_params_dict["bucket_name"] = bucket_name;
                        single_scene_params_dict["stl_bucket_name"] = stl_bucket_name;
                        single_scene_params_dict["ghx_bucket_name"] = ghx_bucket_name;

                        single_scene_params_dict["timeout"] = seconds_timeout;
                        single_scene_params_dict["rhino_visible"] = rhino_visible;
                        single_scene_params_dict["skip_empty_check"] = skip_empty_check;
                        single_scene_params_dict["disable_low_priority"] = disable_low_priority;


                        Start_New_Runner(single_scene_params_dict);

                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void emptyDirTimer_Tick(object sender, EventArgs e)
        {
            String[] dirsToClean = { UtilsDLL.Dirs.images_DirPath, UtilsDLL.Dirs.STL_DirPath };
            foreach (String strDir in dirsToClean)
            {
                if (Directory.Exists(strDir))
                {
                    DirectoryInfo dir = new DirectoryInfo(strDir);
                    FileInfo[] files = dir.GetFiles().OrderBy(p => p.CreationTime).ToArray();
                    DateTime now = DateTime.Now;
                    int deletionsCounter = 0;
                    if (files.Length < 5) continue;
                    foreach (FileInfo file in files)
                    {
                        int minutesAgo = (int)((DateTime.Now - file.CreationTime).TotalMinutes);
                        if (minutesAgo > 5)
                        {
                            try
                            {
                                File.Delete(file.FullName);
                            }
                            catch (Exception exc)
                            {
                                continue;
                            }
                            deletionsCounter++;
                        }
                    }
                    System.Console.WriteLine("Deleted " + deletionsCounter + " files from dir=" + strDir);
                }

            }
        }

    }

    public enum ColumnsIndex
    {
        RUNER_ID = 0,
        SCENE,
        STATE,
        ITEM_ID,
        LAST_DURATION,
        LAST_UPDATE,
        RHINO_PID,
        RUNER_PID,
        REQUEST_URL,
        REQUEST_LOWPRIORITY_URL,
        READY_URL,
        START_CYCLE_TIME,
        JSON_REQUEST,
        ERROR_LINE
    }

}
