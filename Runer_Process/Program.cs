using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using Rhino4;
using System.Diagnostics;
using UtilsDLL;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Drawing;
using System.Net;

namespace Runer_Process
{
    public class ImageDataRequest
    {
        public String bake = null;
        public String item_id;
        public Dictionary<String, Object> propValues = new Dictionary<String, Object>();
        public String gh_fileName;
        public String scene;
        public Size imageSize = new Size();
        public String operation;
        public String layerName = String.Empty;
        public String viewName = String.Empty;
        public bool getSTL = false;
        public String entireJSON;
        public DateTime creationTime;
        public int retries;
        public String stl_to_load;

        public override string ToString()
        {
            String res = "item_id=" + item_id.ToString() + Environment.NewLine;
            res += "gh_fileName=" + gh_fileName + Environment.NewLine;
            res += "rhino_fileName=" + scene + Environment.NewLine;
            res += "bake=" + bake + Environment.NewLine;
            res += "imageSize=" + imageSize.ToString() + Environment.NewLine;
            res += "getSTL=" + getSTL + Environment.NewLine;
            res += "creationTime=" + creationTime.ToShortTimeString() + Environment.NewLine;
            res += "retries=" + retries.ToString() + Environment.NewLine;
            res += "stl_to_load=" + stl_to_load;
            res += "params:" + Environment.NewLine;
            foreach (String key in propValues.Keys)
            {
                res += "    " + key + "=" + propValues[key].ToString() + Environment.NewLine;
            }
            return res;
        }
    }

    public enum CycleResult
    {
        NO_MSG,
        SUCCESS,
        FAIL,
        FUCKUPS_DELETED,
        TIMEOUT
    }

    public enum RenderStatus
    {
        STARTED,
        FINISHED,
        ERROR
    }


    class Program
    {
        private static Semaphore load_rhino_gate;
        private static Semaphore make_cycle_gate;
        private static int whnd;
        private static UtilsDLL.Rhino.Rhino_Wrapper rhino_wrapper;
        public static String current_GH_file = String.Empty;

        public static Dictionary<String, Object> params_dict;

        private static DateTime last_msg_receive_time;
        private static bool delayer = false;
        private static bool useLowPrioirty_Q = false;


        static void log(String str)
        {
            int id = (int)params_dict["id"];
            //Console.WriteLine((id.ToString() + "): Before rhino gate.WaitOne() : " + DateTime.Now.ToString()));
        }

        public static bool deciferImageDataFromBody(String msgBody, out ImageDataRequest imageData)
        {
            imageData = new ImageDataRequest();

            String jsonString = SQS_Utils.DecodeFrom64(msgBody);
            imageData.entireJSON = jsonString;
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var jsonObject = serializer.DeserializeObject(jsonString) as Dictionary<string, object>;
            Dictionary<String, Object> jsonDict = (Dictionary<String, Object>)jsonObject;


            if (!jsonDict.ContainsKey("operation"))
            {
                Console.WriteLine("ERROR !!! - (!jsonDict.ContainsKey(\"operation\"))");
                return false;
            }
            else imageData.operation = (String)jsonDict["operation"];

            if (!jsonDict.ContainsKey("gh_file"))
            {
                Console.WriteLine("ERROR !!! - (!jsonDict.ContainsKey(\"gh_file\"))");
                return false;
            }
            else imageData.gh_fileName = (String)jsonDict["gh_file"];

            if (!jsonDict.ContainsKey("item_id"))
            {
                Console.WriteLine("ERROR !!! - (!jsonDict.ContainsKey(\"item_id\"))");
                return false;
            }
            else imageData.item_id = (String)jsonDict["item_id"];

            if (jsonDict.ContainsKey("scene"))
            {
                imageData.scene = (String)jsonDict["scene"];
            }
            else imageData.scene = null;

            if (!jsonDict.ContainsKey("bake"))
            {
                Console.WriteLine("ERROR !!! - (!jsonDict.ContainsKey(\"bake\"))");
                return false;
            }
            else imageData.bake = (String)jsonDict["bake"];

            if (!jsonDict.ContainsKey("width"))
            {
                Console.WriteLine("INFO - (!jsonDict.ContainsKey(\"width\"))");
                imageData.imageSize.Width = 180;
            }
            else imageData.imageSize.Width = (int)jsonDict["width"];

            if (!jsonDict.ContainsKey("height"))
            {
                Console.WriteLine("INFO - (!jsonDict.ContainsKey(\"height\"))");
                imageData.imageSize.Height = 180;
            }
            else imageData.imageSize.Height = (int)jsonDict["height"];

            if (!jsonDict.ContainsKey("layer_name"))
            {
                imageData.layerName = "Default";
            }
            else
            {
                imageData.layerName = (String)jsonDict["layer_name"];
            }

            if (jsonDict.ContainsKey("getSTL"))
            {
                imageData.getSTL = (bool)jsonDict["getSTL"];
            }


            if (!jsonDict.ContainsKey("view_name"))
            {
                imageData.viewName = "Render";
            }
            else
            {
                imageData.viewName = (String)jsonDict["view_name"];
            }

            if (!jsonDict.ContainsKey("params"))
            {
                Console.WriteLine("ERROR !!! - (!jsonDict.ContainsKey(\"params\"))");
                return false;
            }
            else
            {

                Dictionary<String, Double> paramValues = new Dictionary<string, double>();
                imageData.propValues = (Dictionary<String, Object>)jsonDict["params"];
            }

            if (!jsonDict.ContainsKey("retries"))
            {
                imageData.retries = 1;
            }
            else
            {
                imageData.retries = (int)jsonDict["retries"];
            }


            if (!jsonDict.ContainsKey("load_stl"))
            {
                imageData.stl_to_load = String.Empty;
            }
            else
            {
                imageData.stl_to_load = ((String)jsonDict["load_stl"]).Trim();
            }



            imageData.creationTime = DateTime.Now;
            return true;
        }

        static int id;
        static String scene_fileName;
        static String request_Q_url;
        static String request_lowpriority_Q_url;
        static String ready_Q_url;
        static String error_Q_url;
        static String bucket_name;
        static String stl_bucket_name;

        static int seconds_timeout;
        static bool rhino_visible;
        static IPAddress external_ip;
        static CycleResult lastResult;
        static String lastLogMsg;
        static ImageDataRequest lastIDR;
        static Dictionary<String,Dictionary<String,Dictionary<String,Color[]>>> emptyShortCuts;

        static void Main(string[] args)
        {
            if (!UtilsDLL.Network_Utils.GetIP(out external_ip))
            {
                MessageBox.Show("UtilsDLL.Network_Utils.GetIP() failed!!!. Can not proceed !! Aborting.");
                return;
            }

            UtilsDLL.Dirs.get_all_relevant_dirs();

            whnd = UtilsDLL.Win32_API.FindWindow(null, "RhinoManager");

            Fuckups_DB.Open_Connection();

            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("args[" + i + "]=" + args[i]);
            }


            // Decifer all needed arguments from the command line
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var jsonObject = serializer.DeserializeObject(args[0]) as Dictionary<string, object>;
            params_dict = (Dictionary<String, Object>)jsonObject;

            id = (int)params_dict["id"];
            scene_fileName = (String)params_dict["scene"];
            request_Q_url = (String)params_dict["request_Q_url"];
            request_lowpriority_Q_url = (String)params_dict["request_lowpriority_Q_url"];
            ready_Q_url = (String)params_dict["ready_Q_url"];
            error_Q_url = (String)params_dict["error_Q_url"];
            bucket_name = (String)params_dict["bucket_name"];
            stl_bucket_name = (String)params_dict["stl_bucket_name"];
            rhino_visible = (bool)params_dict["rhino_visible"];
            seconds_timeout = (int)params_dict["timeout"];


            if (!read_empty_images_of_scene(scene_fileName, out emptyShortCuts))
            {
                return;
            }

            // threading semaphore - named global across all machine
            if (id == 99)
            {
                load_rhino_gate = new Semaphore(0, 1, "load_rhino");
                make_cycle_gate = new Semaphore(0, 2, "make_cycle");
                load_rhino_gate.Release(1);
                make_cycle_gate.Release(1);
            }
            else
            {
                load_rhino_gate = Semaphore.OpenExisting("load_rhino");
                make_cycle_gate = Semaphore.OpenExisting("make_cycle");
            }


            UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Waiting_Rhino");

            log("Before rhino gate.WaitOne() : " + DateTime.Now.ToString());
            // We load the Rhinos one bye one to make sure what is their PID
            // before loading Rhinos ...
            load_rhino_gate.WaitOne();

            log("After rhino gate.WaitOne() : " + DateTime.Now.ToString());
            log("Before rhino creation : " + DateTime.Now.ToString());

            UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Started_Rhino");

            if (!UtilsDLL.Rhino.start_a_SingleRhino(scene_fileName, rhino_visible, out rhino_wrapper))
            {
                log("startSingleRhino() failed");
                MessageBox.Show("Basa");
                load_rhino_gate.Release();
                return;
            }

            log("): After rhino creation : " + DateTime.Now.ToString());
            // get a list with new Rhino.. that was not there before..

            UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Finished_Rhino " + rhino_wrapper.rhino_pid);
            //sw.Write("Finished Rhino startup");

            log("): Before rhino gate.Release() : " + DateTime.Now.ToString());
            load_rhino_gate.Release();
            log("): After rhino gate.Release() : " + DateTime.Now.ToString());

            last_msg_receive_time = DateTime.Now;

            while (true)
            {
                log("): Before cycle gate.Wait one() : " + DateTime.Now.ToString());
                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Waiting gate");
                make_cycle_gate.WaitOne();
                log("): After cycle gate.Wait one() : " + DateTime.Now.ToString());
                DateTime beforeProcessingTime = DateTime.Now;
                ThreadStart ts = new ThreadStart(single_cycle);
                Thread thread = new Thread(ts);
                thread.Start();
                thread.Join(seconds_timeout * 1000);
                switch (lastResult)
                {
                    case CycleResult.FAIL:
                    case CycleResult.TIMEOUT:
                        // do NOT release the lock - simply send an error msg to the window and stop. Releasing will be done at Manager level
                        String msgToSend = String.Empty;
                        if (lastResult == CycleResult.TIMEOUT) msgToSend += "TIMEOUT!!! ";
                        msgToSend += lastLogMsg;
                        UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "ERROR " + msgToSend);
                        return;
                    case CycleResult.FUCKUPS_DELETED:
                        UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "FUCKUP DELETED");
                        if (lastIDR == null)
                        {
                            Send_Msg_To_ERROR_Q(lastLogMsg);
                        }
                        else
                        {
                            TimeSpan duration = DateTime.Now - lastIDR.creationTime;
                            Dictionary<String,Object> dict = new Dictionary<string,object>();
                            dict["item_id"] = lastIDR.item_id;
                            dict["url"] = @"http://s3.amazonaws.com/" + bucket_name + @"/" + lastIDR.item_id + ".jpg";
                            dict["duration"] = Math.Round(duration.TotalSeconds, 3);
                            dict["server"] = external_ip.ToString();
                            dict["instance_id"] = id;
                            dict["status"] = "ERROR";

                            Send_Dict_Msg_To_Readies_Q(dict);
                        }
                        break;
                    case CycleResult.NO_MSG:
                        TimeSpan timeFromLastMsg = DateTime.Now - last_msg_receive_time;
                        if (timeFromLastMsg.TotalSeconds > 60)
                        {
                            if (!delayer)
                            {
                                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "DELAYER_ON");
                            }
                            delayer = true;
                        }
                        break;
                    default:
                        if (!useLowPrioirty_Q)
                        {
                            if (delayer)
                            {
                                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "DELAYER_OFF");
                            }
                            delayer = false;
                            last_msg_receive_time = DateTime.Now;
                        }
                        break;
                }

                //Console.WriteLine(id + "): Before cycle gate.Release() : " + DateTime.Now.ToString());
                make_cycle_gate.Release();
                //Console.WriteLine(id + "): After cycle gate.Release() : " + DateTime.Now.ToString());
                if (delayer)
                {
                    if (!useLowPrioirty_Q) Thread.Sleep(2000);
                }
            }


        }

        private static bool read_empty_images_of_scene(String scene, out Dictionary<String,Dictionary<String,Dictionary<String,Color[]>>> shortCuts)
        {
            shortCuts = new Dictionary<string, Dictionary<string, Dictionary<string, Color[]>>>();
            try
            {
                DirectoryInfo dir = new DirectoryInfo(UtilsDLL.Dirs.empty_images_DirPath);
                DirectoryInfo[] dirs = dir.GetDirectories(scene);
                if (dirs.Length != 1) return false;
//                foreach (DirectoryInfo gh_dir in dirs[0].GetDirectories())
//                {
                    foreach (DirectoryInfo size_dir in dirs[0].GetDirectories())
                    {
                        shortCuts[size_dir.Name] = new Dictionary<string, Dictionary<string, Color[]>>();
                        foreach (DirectoryInfo view_dir in size_dir.GetDirectories())
                        {
                            shortCuts[size_dir.Name][view_dir.Name] = new Dictionary<string, Color[]>();
                            foreach (FileInfo emptyImageFile in view_dir.GetFiles("*.jpg"))
                            {
                                Color[] shortCut;
                                if (!UtilsDLL.Image_Utils.shortCut(emptyImageFile.FullName, out shortCut))
                                {
                                    return false;
                                }
                                shortCuts[size_dir.Name][view_dir.Name][emptyImageFile.Name] = shortCut;

                            }
                        }
                    }
//                }


                FileInfo[] files = dir.GetFiles("*.jpg");
                foreach (FileInfo file in files)
                {
                    if (file.Name.StartsWith(scene))
                    {
                    }
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
            
            return true;
        }

        private static void single_cycle()
        {
            lastResult = CycleResult.TIMEOUT;
            lastLogMsg = "Starting single_cycle";
            lastIDR = null;

            // Get a single MSG from Queue_Requests
            bool msg_found;
            Amazon.SQS.Model.Message msg;
            useLowPrioirty_Q = false;
            if (!SQS_Utils.Get_Msg_From_Q(request_Q_url, out msg, out msg_found))
            {
                lastLogMsg = "Get_Msg_From_Q() failed !!!";
                log(lastLogMsg);
                lastResult = CycleResult.FAIL;
                return;
            }

            DateTime beforeProcessingTime = DateTime.Now;
            // if there is No Msg - Sleep & continue;
            if (!msg_found)
            {
                if (delayer) // this means we may also check on the lowprioirty Q
                {
                    bool lowPrioirty_msg_found = false;
                    if (!SQS_Utils.Get_Msg_From_Q(request_lowpriority_Q_url, out msg, out lowPrioirty_msg_found))
                    {
                        lastLogMsg = "Get_Msg_From_Q(lowprioirty) failed !!!";
                        log(lastLogMsg);
                        lastResult = CycleResult.FAIL;
                        return;
                    }

                    if (!lowPrioirty_msg_found)
                    {
                        lastLogMsg = "No MSG";
                        UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "no_msg");
                        lastResult = CycleResult.NO_MSG;
                        return;
                    }
                    useLowPrioirty_Q = true;
                }
                else
                {
                    lastLogMsg = "No MSG";
                    UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "no_msg");
                    lastResult = CycleResult.NO_MSG;
                    return; 
                }
            }


            // Extract the ImageData
            ImageDataRequest imageData = null;
            if (!deciferImageDataFromBody(msg.Body, out imageData))
            {
                lastLogMsg = "deciferImagesDataFromJSON(msg.Body=" + msg.Body + ", out imagesDatas) failed!!!";
                log(lastLogMsg);
                lastResult = CycleResult.FAIL;
                return;
            }

            lastIDR = imageData;


            int prevFuckups_this_image = Fuckups_DB.Get_Fuckups(imageData.item_id);

            if (prevFuckups_this_image >= imageData.retries)
            {
                // send an error msg telling that this image was deleted
                // delete the message...
                Delete_Msg_From_Req_Q(msg,useLowPrioirty_Q);
                lastResult = CycleResult.FUCKUPS_DELETED;
                return;
            }

            if (prevFuckups_this_image > 0)
            {
                adjust_numeric_params(imageData.propValues);
            }

            // Add Msg to Queue_Readies
            DateTime current = DateTime.Now;
            TimeSpan duration = current - beforeProcessingTime;
            Dictionary<String, Object> tempDict = new Dictionary<string, object>();
            tempDict["item_id"] = imageData.item_id;
            //dict["url"] = @"http://" + Utils.my_ip + @"/testim/yofi_" + item_id + ".jpg";
            tempDict["url"] = @"http://s3.amazonaws.com/" + bucket_name + @"/" + imageData.item_id + ".jpg";
            tempDict["duration"] = Math.Round(duration.TotalSeconds, 3);
            tempDict["status"] = RenderStatus.STARTED.ToString();

            if (!Send_Dict_Msg_To_Readies_Q(tempDict))
            {
                lastLogMsg = "Send_Msg_To_Readies_Q(status=STARTED,imageData.item_id=" + imageData.item_id + ") failed";
                log(lastLogMsg);
                lastResult = CycleResult.FAIL;
                return;
            }


            if (imageData.operation == "render_model")
            {
                Console.WriteLine("EntireJSON = " + imageData.entireJSON);
                // inform manager
                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "render_model starting " + imageData.item_id + " " + imageData.entireJSON);

                DateTime beforeRhino = DateTime.Now;
                // Process Msg to picture
                String resultingLocalImageFilePath;
                TimeSpan renderTime, buildTime, waitTime;
                if (!Process_Into_Image_File(imageData, out resultingLocalImageFilePath, out renderTime, out buildTime, out waitTime))
                {
                    String logLine = "Process_Msg_Into_Image_File(msg) failed!!! (). ImageData=" + imageData.ToString();
                    lastLogMsg = logLine;
                    log(logLine);
//                    Send_Msg_To_ERROR_Q(imageData.item_id, logLine, beforeProcessingTime);
                    lastResult = CycleResult.FAIL;
                    return;
                }


                // check with empty images
                Color[] shortCut;
                if (!UtilsDLL.Image_Utils.shortCut(resultingLocalImageFilePath,out shortCut))
                {
                    String logLine = "UtilsDLL.Image_Utils.shortCut(file=" + resultingLocalImageFilePath+"  failed!!! ().";
                    lastLogMsg = logLine;
                    log(logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }

                

                String size_key = imageData.imageSize.Width + "_" + imageData.imageSize.Height;
                if (!emptyShortCuts.ContainsKey(size_key))
                {
                    String logLine = "Found no empty image file to compare to (size_key=" + size_key + " )!!";
                    lastLogMsg = logLine;
                    log(logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }

                if (!emptyShortCuts[size_key].ContainsKey(imageData.viewName))
                {
                    String logLine = "Found no empty image file to compare to (viewName=" + imageData.viewName + " )!!";
                    lastLogMsg = logLine;
                    log(logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }

                if (emptyShortCuts[size_key][imageData.viewName].Count == 0)
                {
                    String logLine = "Found no empty image file to compare to (Count==0)!!";
                    lastLogMsg = logLine;
                    log(logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }

                foreach (String key in emptyShortCuts[size_key][imageData.viewName].Keys)
                {
                    Color[] emptyImageSC = emptyShortCuts[size_key][imageData.viewName][key];
                    bool compRes = false;
                    if (!UtilsDLL.Image_Utils.compare_shortcuts(shortCut, emptyImageSC, out compRes))
                    {
                        String logLine = "Failed because comparing failed rendered image (" + resultingLocalImageFilePath + ") to  file:" + imageData.gh_fileName + Path.DirectorySeparatorChar + size_key + Path.DirectorySeparatorChar + imageData.viewName + Path.DirectorySeparatorChar + key;
                        lastLogMsg = logLine;
                        log(logLine);
                        lastResult = CycleResult.FAIL;
                        return;
                    }
                    if (compRes)
                    {
                        String logLine = "Failed because rendered image (" + resultingLocalImageFilePath + ") identical to empty image file:" + imageData.gh_fileName + Path.DirectorySeparatorChar + size_key + Path.DirectorySeparatorChar + imageData.viewName + Path.DirectorySeparatorChar + key;
                        lastLogMsg = logLine;
                        log(logLine);
                        lastResult = CycleResult.FAIL;
                        return;
                    }
                }
                


/*
                if (imageData.item_id.EndsWith("22"))
                {
                    String logLine = "Delibiretly failing item - ends with 22. ImageData=" + imageData.ToString();
                    lastLogMsg = logLine;
                    log(logLine);
                    MessageBox.Show(logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }
*/
                
                TimeSpan stl_timespan = new TimeSpan(0,0,0,99);
                if (imageData.getSTL)
                {
                    DateTime beforeSTL = DateTime.Now;
                    String resulting_3dm_path = resultingLocalImageFilePath.Replace(".jpg",".3dm");
                    String command = "-SelAll";
                    rhino_wrapper.rhino_app.RunScript(command, 1);
                    command = "-Export _GeometryOnly=Yes " + resulting_3dm_path;
                    rhino_wrapper.rhino_app.RunScript(command, 1);
                    //rhino_wrapper.rhino_app.RunScript("-SaveAs " + resulting_3dm_path +" Enter Enter", 1);
                    stl_timespan = DateTime.Now - beforeSTL;

                    String stl_fileName_on_S3 = imageData.item_id.ToString() + ".3dm";
                    if (!S3_Utils.Write_File_To_S3(stl_bucket_name, resulting_3dm_path, stl_fileName_on_S3))
                    {
                        String logLine = "Write_File_To_S3(resulting_3dm_path=" + resulting_3dm_path + ", stl_fileName_on_S3=" + stl_fileName_on_S3 + ") failed !!!";
                        log(logLine);
                        lastLogMsg = logLine;
//                        Send_Msg_To_ERROR_Q(imageData.item_id, logLine, beforeProcessingTime);
                        lastResult = CycleResult.FAIL;
                        return;
                    }
                }

                DateTime afterRhino_Before_S3 = DateTime.Now;

                String fileName_on_S3 = imageData.item_id.ToString() + ".jpg";
                if (!S3_Utils.Write_File_To_S3(bucket_name, resultingLocalImageFilePath, fileName_on_S3))
                {
                    String logLine = "Write_File_To_S3(resultingImagePath=" + resultingLocalImageFilePath + ", fileName_on_S3=" + fileName_on_S3 + ") failed !!!";
                    log(logLine);
                    lastLogMsg = logLine;
//                    Send_Msg_To_ERROR_Q(imageData.item_id, logLine, beforeProcessingTime);
                    lastResult = CycleResult.FAIL;
                    return;
                }


                DateTime afterS3_Before_SQS = DateTime.Now;

                // Delete Msg From Queue_Requests
                if (!Delete_Msg_From_Req_Q(msg,useLowPrioirty_Q))
                {
                    String logLine = "Delete_Msg_From_Req_Q(item_id=" + imageData.item_id + ") failed!!!";
                    log(logLine);
                    lastLogMsg = logLine;
//                    Send_Msg_To_ERROR_Q(imageData.item_id, logLine, beforeProcessingTime);
/*
                    if (!Send_Msg_To_Readies_Q(RenderStatus.ERROR, imageData.item_id, beforeProcessingTime))
                    {
                        logLine = ("Send_Msg_To_Readies_Q(status=ERROR,imageData.item_id=" + imageData.item_id + ") failed");
                        log(logLine);
                        Send_Msg_To_ERROR_Q(imageData.item_id, logLine, beforeProcessingTime);
                    }
 */
                    lastResult = CycleResult.FAIL;
                    return;
                }


                // Add Msg to Queue_Readies
                current = DateTime.Now;
                duration = current - beforeProcessingTime;
                tempDict = new Dictionary<string, object>();
                tempDict["item_id"] = imageData.item_id;
                tempDict["url"] = @"http://s3.amazonaws.com/" + bucket_name + @"/" + imageData.item_id + ".jpg";
                tempDict["duration"] = Math.Round(duration.TotalSeconds, 3);
                tempDict["status"] = RenderStatus.FINISHED.ToString();


                if (!Send_Dict_Msg_To_Readies_Q(tempDict))
                {
                    lastLogMsg = "Send_Dict_Msg_To_Readies_Q(status=FINISHED,imageData.item_id=" + imageData.item_id + ") failed";
                    log(lastLogMsg);
                    lastResult = CycleResult.FAIL;
                    return;
                }

/*
                // Add Msg to Queue_Readies
                if (!Send_Msg_To_Readies_Q(RenderStatus.FINISHED, imageData.item_id, beforeProcessingTime))
                {
                    String logLine = "Send_Msg_To_Readies_Q(imageData.item_id=" + imageData.item_id + ") failed";
                    log(logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }
*/
                DateTime afterSQS = DateTime.Now;

                log("Reading msg time =" + (beforeRhino - beforeProcessingTime).TotalMilliseconds.ToString() + " millis");
                log("Wait time=" + waitTime.TotalMilliseconds.ToString() + " millis");
                log("Rhino build time=" + buildTime.TotalMilliseconds.ToString() + " millis");
                log("Render time=" + renderTime.TotalMilliseconds.ToString() + " millis");
                if (imageData.getSTL)
                {
                    log("getSTL time=" + stl_timespan.TotalMilliseconds.ToString() + " millis");
                }
                log("S3 Time=" + (afterS3_Before_SQS - afterRhino_Before_S3).TotalMilliseconds.ToString() + " millis");
                log("SQS Time=" + (afterSQS - afterS3_Before_SQS).TotalMilliseconds.ToString() + " millis");

                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "render_model finished " + imageData.item_id);
                lastResult = CycleResult.SUCCESS;
                return;
            }
            else
            {
                lastLogMsg = "ERROR !!! - (" + imageData.operation + "=imageData.operation != \"render_model\")";
                log(lastLogMsg);
                lastResult = CycleResult.FAIL;
                return;
            }
        }

        private static void adjust_numeric_params(Dictionary<string, object> dictionary)
        {
            String[] keys = new String[dictionary.Count];
            dictionary.Keys.CopyTo(keys, 0);
            foreach (String key in keys)
            {
                Object propValue = dictionary[key];
                Type propValueType = propValue.GetType();
                if (propValueType == typeof(Double) || propValueType == typeof(Decimal))
                {
                    Double oldValue;
                    if (propValueType == typeof(Double)) oldValue = (Double)propValue;
                    else oldValue = (Double)((Decimal)propValue);

                    Double newValue = oldValue;
                    if (oldValue < 0.01) newValue = 0.05;
                    else if (oldValue > 0.99) newValue = 0.95;
                    else
                    {
                        Random rnd = new Random();
                        Double d = rnd.NextDouble();
                        if (d > 0.5) newValue += 0.05;
                        else newValue -= 0.05;
                    }

                    dictionary[key] = newValue;
                }
            }
        }

        private static bool Delete_Msg_From_Req_Q(Amazon.SQS.Model.Message msg, bool useLowPrioirty_Q)
        {
            if (useLowPrioirty_Q) return SQS_Utils.Delete_Msg_From_Q(request_lowpriority_Q_url, msg);
            return SQS_Utils.Delete_Msg_From_Q(request_Q_url, msg);
        }

        private static void Send_Msg_To_ERROR_Q(string err_msg)
        {
            SQS_Utils.Send_Msg_To_Q(error_Q_url, err_msg, false);
        }

        public static bool Process_Into_Image_File(ImageDataRequest imageData, out string resultingLocalImageFilePath, out TimeSpan renderTime, out TimeSpan buildTime, out TimeSpan waitTime)
        {
            DateTime beforeTime = DateTime.Now;

            String logLine = "Starting Process_Into_Image_File()";
            log(logLine);
            resultingLocalImageFilePath = String.Empty;
            renderTime = new TimeSpan();
            buildTime = new TimeSpan();
            waitTime = new TimeSpan();

            if (!UtilsDLL.Rhino.DeleteAll(rhino_wrapper))
            {
                log("ERROR!!: DeleteAll() failed !!!");
                return false;
            }

            if (!Rhino.setDefaultLayer(rhino_wrapper, imageData.layerName))
            {
                log("ERROR!!: Rhino.setDefaultLayer(layerName=" + imageData.layerName + ") failed !!!");
                return false;
            }



            if (imageData.gh_fileName.EndsWith(".gh") || imageData.gh_fileName.EndsWith(".ghx"))
            {
/*
                if (!UtilsDLL.Rhino.Set_GH_Params_To_TXT_File(rhino_wrapper, imageData.propValues))
                {
                    logLine = "Set_Params(imageData=" + imageData.ToString() + "]) failed !!!";
                    log(logLine);
                    return false;
                }
*/
                if (current_GH_file == imageData.gh_fileName)
                {
                    logLine = "Skipping Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_fileName + ")";
                    log(logLine);
                }
                else
                {
                    if (!UtilsDLL.Rhino.Open_GH_File(rhino_wrapper, Dirs.GH_DirPath + Path.DirectorySeparatorChar + imageData.gh_fileName))
                    {
                        logLine = "Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_fileName + "); failed";
                        log(logLine);
                        return false;
                    }
                    current_GH_file = imageData.gh_fileName;

                    if (!UtilsDLL.Rhino.DeleteAll(rhino_wrapper))
                    {
                        log("ERROR!!: DeleteAll(after new GH file) failed !!!");
                        return false;
                    }
                
                
                }

                // had to split the execution of stl_to_load to after theabove Delete_All
                // (of new Grasshopper files- that may write directly junk data to rhino file)
                // therefore LOAD_stl had to be split between GH and script options
                if (!String.IsNullOrWhiteSpace(imageData.stl_to_load))
                {
                    if (!load_stl(imageData.stl_to_load))
                    {
                        log("ERROR!!: load_stl(imageData.stl_to_load="+imageData.stl_to_load+") failed !!!");
                        return false;
                    }
                }


                if (!UtilsDLL.Rhino.Set_GH_Params(rhino_wrapper,imageData.propValues))
                {
                    logLine = "Set_Params(imageData=" + imageData.ToString() + "]) failed !!!";
                    log(logLine);
                    return false;
                }
                if (!UtilsDLL.Rhino.Solve_And_Bake(rhino_wrapper, imageData.bake))
                {
                    logLine = "Solve_And_Bake(imageData=" + imageData.ToString() + "]) failed !!!";
                    log(logLine);
                    return false;
                }

            }
            else
            {
                // had to split the execution of stl_to_load to after theabove Delete_All
                // (of new Grasshopper files- that may write directly junk data to rhino file)
                // therefore LOAD_stl had to be split between GH and script options
                if (!String.IsNullOrWhiteSpace(imageData.stl_to_load))
                {
                    if (!load_stl(imageData.stl_to_load))
                    {
                        log("ERROR!!: load_stl(imageData.stl_to_load=" + imageData.stl_to_load + ") failed !!!");
                        return false;
                    }
                }

                if (!UtilsDLL.Rhino.Run_Script(rhino_wrapper, imageData.gh_fileName, imageData.propValues))
                {
                    logLine = "Run_Script(imageData=" + imageData.ToString() + "]) failed !!!";
                    log(logLine);
                    return false;
                }
            }


            buildTime = DateTime.Now - beforeTime;


            String resultingImagePath = Dirs.images_DirPath + Path.DirectorySeparatorChar + "yofi_" + imageData.item_id + ".jpg";
            if (!Rhino.Render(rhino_wrapper, imageData.viewName, imageData.imageSize, resultingImagePath))
            {
                log("Render(imageData=" + imageData.ToString() + ") failed !!!");
                return false;
            }

            renderTime = (DateTime.Now - beforeTime) - buildTime;
            resultingLocalImageFilePath = resultingImagePath;

            DateTime afterTime = DateTime.Now;
            int timed = (int)((afterTime - beforeTime).TotalMilliseconds);
            log("Total Get_Pictures() call took " + timed + " millseconds");
            return true;

        }

        private static bool load_stl(string stl_to_load)
        {
            String stlFileName = stl_to_load.Trim();
            if (!stlFileName.EndsWith(".3dm")) stlFileName += ".3dm";
            String stl_local_path = UtilsDLL.Dirs.STL_DirPath + Path.DirectorySeparatorChar + stlFileName;
            if (!File.Exists(stl_local_path))
            {
                if (!S3_Utils.Download_File_From_S3(stl_bucket_name, stl_local_path, stlFileName))
                {
                    log("ERROR!!: S3_Utils.Download_File_From_S3(stl_bucket_name=" + stl_bucket_name + ", stl_local_path=" + stl_local_path + ", stlFileName=" + stlFileName + ") failed !!!");
                    return false;
                }
            }
            if (!Rhino.Load_STL(rhino_wrapper, stl_local_path))
            {
                log("ERROR!!: Rhino.Load_STL(stl_to_load=" + stl_to_load + ") failed !!!");
                MessageBox.Show("111");
                return false;
            }
            return true;
        }






/*
        private static bool Send_Msg_To_Readies_Q(RenderStatus renderStatus, string item_id, DateTime beforeProcessingTime)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            DateTime current = DateTime.Now;
            TimeSpan duration = current - beforeProcessingTime;

            dict["item_id"] = item_id;
            //dict["url"] = @"http://" + Utils.my_ip + @"/testim/yofi_" + item_id + ".jpg";
            dict["url"] = @"http://s3.amazonaws.com/" + bucket_name + @"/" + item_id + ".jpg";
            dict["duration"] = Math.Round(duration.TotalSeconds, 3);
            dict["status"] = renderStatus.ToString();

            JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
            string jsonString = serializer.Serialize((object)dict);

            return SQS_Utils.Send_Msg_To_Q(ready_Q_url, jsonString, true);
        }
*/
        private static bool Send_Dict_Msg_To_Readies_Q(Dictionary<String,Object> dict)
        {

            JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
            string jsonString = serializer.Serialize((object)dict);

            return SQS_Utils.Send_Msg_To_Q(ready_Q_url, jsonString, true);
        }

    }
}
