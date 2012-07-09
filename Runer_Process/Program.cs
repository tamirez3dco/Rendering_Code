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


        public override string ToString()
        {
            String res = "item_id=" + item_id.ToString() + Environment.NewLine;
            res += "gh_fileName=" + gh_fileName + Environment.NewLine;
            res += "rhino_fileName=" + scene + Environment.NewLine;
            res += "bake=" + bake + Environment.NewLine;
            res += "imageSize=" + imageSize.ToString() + Environment.NewLine;
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
        FUCKUPS_DELETED
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


        static void log(String str)
        {
            int id = (int)params_dict["id"];
            Console.WriteLine((id.ToString() + "): Before rhino gate.WaitOne() : " + DateTime.Now.ToString()));
        }

        public static bool deciferImageDataFromBody(String msgBody, out ImageDataRequest imageData)
        {
            imageData = new ImageDataRequest();

            String jsonString = SQS_Utils.DecodeFrom64(msgBody);

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
            return true;
        }


        static int id;
        static String scene_fileName;
        static String request_Q_url;
        static String ready_Q_url;
        static String error_Q_url;
        static String bucket_name;
        static int seconds_timeout;
        static bool rhino_visible;

        static CycleResult lastResult;

        static void Main(string[] args)
        {
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
            ready_Q_url = (String)params_dict["ready_Q_url"];
            error_Q_url = (String)params_dict["error_Q_url"];
            bucket_name = (String)params_dict["bucket_name"];
            rhino_visible = (bool)params_dict["rhino_visible"];
            seconds_timeout = (int)params_dict["timeout"];

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


            while (true)
            {
                log("): Before cycle gate.Wait one() : " + DateTime.Now.ToString());
                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "Waiting gate");
                make_cycle_gate.WaitOne();
                log("): After cycle gate.Wait one() : " + DateTime.Now.ToString());

                ThreadStart ts = new ThreadStart(single_cycle);
                Thread thread = new Thread(ts);
                thread.Start();
                thread.Join(seconds_timeout * 1000);
                switch (lastResult)
                {
                    case CycleResult.FAIL:
                        // do NOT release the lock - simply send an error msg to the window and stop. Releasing will be done at Manager level
                        UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "ERROR");
                        return;
                    case CycleResult.FUCKUPS_DELETED:
                        UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "FUCKUP DELETED");
                        break;
                }

                Console.WriteLine(id + "): Before cycle gate.Release() : " + DateTime.Now.ToString());
                make_cycle_gate.Release();
                Console.WriteLine(id + "): After cycle gate.Release() : " + DateTime.Now.ToString());
            }


        }

        private static void single_cycle()
        {
            lastResult = CycleResult.FAIL;

            // Get a single MSG from Queue_Requests
            bool msg_found;
            Amazon.SQS.Model.Message msg;
            if (!SQS_Utils.Get_Msg_From_Q(request_Q_url, out msg, out msg_found))
            {
                log("Get_Msg_From_Q() failed !!!");
                lastResult = CycleResult.FAIL;
                return;
            }

            DateTime beforeProcessingTime = DateTime.Now;

            // if there is No Msg - Sleep & continue;
            if (!msg_found)
            {
                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "no_msg");
                lastResult = CycleResult.NO_MSG;
                return;
            }


            // Extract the ImageData
            ImageDataRequest imageData = null;
            if (!deciferImageDataFromBody(msg.Body, out imageData))
            {
                log("deciferImagesDataFromJSON(msg.Body=" + msg.Body + ", out imagesDatas) failed!!!");
                lastResult = CycleResult.FAIL;
                return;
            }

            int prevFuckups_this_image = Fuckups_DB.Get_Fuckups(imageData.item_id);
            if (prevFuckups_this_image >= 2)
            {
                // send an error msg telling that this image was deleted
                Send_Msg_To_ERROR_Q(id, "Item_id:" + imageData.item_id + " was deleted from request Q without rendering because prevFuckups_this_image=" + prevFuckups_this_image.ToString());
                // delete the message...
                Delete_Msg_From_Req_Q(msg);
                lastResult = CycleResult.FUCKUPS_DELETED;
                return;
            }

            // Add Msg to Queue_Readies
            if (!Send_Msg_To_Readies_Q(RenderStatus.STARTED, imageData.item_id, beforeProcessingTime))
            {
                log("Send_Msg_To_Readies_Q(status=STARTED,imageData.item_id=" + imageData.item_id + ") failed");
                lastResult = CycleResult.FAIL;
                return;
            }


            if (imageData.operation == "render_model")
            {
                // inform manager
                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "render_model starting " + imageData.item_id);

                DateTime beforeRhino = DateTime.Now;
                // Process Msg to picture
                String resultingLocalImageFilePath;
                TimeSpan renderTime, buildTime, waitTime;
                if (!Process_Into_Image_File(imageData, out resultingLocalImageFilePath, out renderTime, out buildTime, out waitTime))
                {
                    log("Process_Msg_Into_Image_File(msg) failed!!! (). ImageData=" + imageData.ToString());
                    Send_Msg_To_ERROR_Q(id, imageData.item_id);
                    lastResult = CycleResult.FAIL;
                    return;
                }

                DateTime afterRhino_Before_S3 = DateTime.Now;
                int id_as_int_debug;
                if (int.TryParse(imageData.item_id,out id_as_int_debug))
                {
                    if (id_as_int_debug % 10 == 5)
                    {
                        DialogResult dres = MessageBox.Show("image " + imageData.item_id + " halting...", "stam", MessageBoxButtons.YesNoCancel);
                        if (DialogResult.Yes == dres)
                        {
                            Win32_API.Kill_Process(rhino_wrapper.rhino_pid);
                            int pid = Process.GetCurrentProcess().Id;
                            Win32_API.Kill_Process(pid);
                        }
                        else if (DialogResult.No == dres)
                        {
                            lastResult = CycleResult.FAIL;
                            return;
                        }
                    }
                }

                String fileName_on_S3 = imageData.item_id.ToString() + ".jpg";
                if (!S3_Utils.Write_File_To_S3(bucket_name, resultingLocalImageFilePath, fileName_on_S3))
                {
                    String logLine = "Write_File_To_S3(resultingImagePath=" + resultingLocalImageFilePath + ", fileName_on_S3=" + fileName_on_S3 + ") failed !!!";
                    log(logLine);
                    Send_Msg_To_ERROR_Q(id, logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }

                DateTime afterS3_Before_SQS = DateTime.Now;

                // Delete Msg From Queue_Requests
                if (!Delete_Msg_From_Req_Q(msg))
                {
                    String logLine = "Delete_Msg_From_Req_Q(item_id=" + imageData.item_id + ") failed!!!";
                    log(logLine);
                    Send_Msg_To_ERROR_Q(id, logLine);

                    if (!Send_Msg_To_Readies_Q(RenderStatus.ERROR, imageData.item_id, beforeProcessingTime))
                    {
                        logLine = ("Send_Msg_To_Readies_Q(status=ERROR,imageData.item_id=" + imageData.item_id + ") failed");
                        log(logLine);
                        Send_Msg_To_ERROR_Q(id, logLine);
                    }
                    lastResult = CycleResult.FAIL;
                    return;
                }

                // Add Msg to Queue_Readies
                if (!Send_Msg_To_Readies_Q(RenderStatus.FINISHED, imageData.item_id, beforeProcessingTime))
                {
                    String logLine = "Send_Msg_To_Readies_Q(imageData.item_id=" + imageData.item_id + ") failed";
                    log(logLine);
                    lastResult = CycleResult.FAIL;
                    return;
                }

                DateTime afterSQS = DateTime.Now;

                log("Reading msg time =" + (beforeRhino - beforeProcessingTime).TotalMilliseconds.ToString() + " millis");
                log("Wait time=" + waitTime.TotalMilliseconds.ToString() + " millis");
                log("Rhino build time=" + buildTime.TotalMilliseconds.ToString() + " millis");
                log("Render time=" + renderTime.TotalMilliseconds.ToString() + " millis");
                log("S3 Time=" + (afterS3_Before_SQS - afterRhino_Before_S3).TotalMilliseconds.ToString() + " millis");
                log("SQS Time=" + (afterSQS - afterS3_Before_SQS).TotalMilliseconds.ToString() + " millis");

                UtilsDLL.Win32_API.sendWindowsStringMessage(whnd, id, "render_model finished " + imageData.item_id);
                lastResult = CycleResult.SUCCESS;
                return;
            }
            else
            {
                log("ERROR !!! - (" + imageData.operation + "=imageData.operation != \"render_model\")");
                lastResult = CycleResult.FAIL;
                return;
            }
        }

        private static bool Delete_Msg_From_Req_Q(Amazon.SQS.Model.Message msg)
        {
            return SQS_Utils.Delete_Msg_From_Q(request_Q_url, msg);
        }

        private static void Send_Msg_To_ERROR_Q(int id, string err_msg)
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

            if (!DeleteAll())
            {
                log("ERROR!!: DeleteAll() failed !!!");
                return false;
            }

            if (!setDefaultLayer(imageData.layerName))
            {
                log("ERROR!!: setDefaultLayer(layerName=" + imageData.layerName + ") failed !!!");
                return false;
            }

            if (imageData.gh_fileName.EndsWith(".gh") || imageData.gh_fileName.EndsWith(".ghx"))
            {


                if (current_GH_file == imageData.gh_fileName)
                {
                    logLine = "Skipping Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_fileName + ")";
                    log(logLine);
                }
                else
                {
                    if (!Open_GH_File(Dirs.GH_DirPath + Path.DirectorySeparatorChar + imageData.gh_fileName))
                    {
                        logLine = "Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_fileName + "); failed";
                        log(logLine);
                        return false;
                    }
                    current_GH_file = imageData.gh_fileName;
                }


                if (!Set_GH_Params(imageData))
                {
                    logLine = "Set_Params(imageData=" + imageData.ToString() + "]) failed !!!";
                    log(logLine);
                    return false;
                }
            }
            else
            {
                if (!Run_Script(imageData))
                {
                    logLine = "Run_Script(imageData=" + imageData.ToString() + "]) failed !!!";
                    log(logLine);
                    return false;
                }
            }


            buildTime = DateTime.Now - beforeTime;


            String resultingImagePath;
            if (!Render(imageData, out resultingImagePath))
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

        private static bool Open_GH_File(string filePath)
        {
            log("Starting  Open_GH_File(*,filePath=" + filePath);
            DateTime before = DateTime.Now;

            try
            {
                rhino_wrapper.grasshopper.CloseAllDocuments();
                Thread.Sleep(1000);
                rhino_wrapper.grasshopper.OpenDocument(filePath);
            }
            catch (Exception e)
            {
                log("Exception=" + e.Message);
                return false;
            }

            log("Finished succefully  Open_GH_File(*,filePath=" + filePath + ((int)(DateTime.Now - before).TotalMilliseconds) + " miliseconds after Starting");
            return true;

        }

        private static bool Render(ImageDataRequest imageData, out string resultingImagePath)
        {
            resultingImagePath = Dirs.images_DirPath + Path.DirectorySeparatorChar + "yofi_" + imageData.item_id + ".jpg";
            try
            {

                DateTime beforeTime = DateTime.Now;
                String captureCommand = "-FlamingoRenderTo f " + resultingImagePath + " " + imageData.imageSize.Width + " " + imageData.imageSize.Height;
                int captureCommandRes = rhino_wrapper.rhino_app.RunScript(captureCommand, 1);

                int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                log("After rendering by: " + captureCommand + "  into" + resultingImagePath + " After " + fromStart + " milliseconds");
            }
            catch (Exception e)
            {
                log("Excpetion in Render(imageData=" + imageData.ToString() + ", String outputPath=" + resultingImagePath + ", e.Message=" + e.Message);
                return false;
            }

            return true;

        }

        private static bool Run_Script(ImageDataRequest imageData)
        {
            log("Starting Run_Script_And_Render(ImageData imageData=" + imageData.ToString() + "))");
            DateTime beforeTime = DateTime.Now;
            try
            {
                String commParams = "";
                List<String> stringValues = new List<string>();
                foreach (String paramName in imageData.propValues.Keys)
                {
                    Object propValue = imageData.propValues[paramName];
                    Type propValueType = propValue.GetType();
                    if (propValueType == typeof(Double) || propValueType == typeof(Decimal))
                    {
                        commParams = commParams + " " + paramName + "=" + imageData.propValues[paramName].ToString();
                    }
                    else if (propValue.GetType() == typeof(String))
                    {
                        stringValues.Add((String)propValue);
                    }
                }

                //String runCommand = "vase1 rad1=0.2 rad2=0.42 rad3=0.6 rad4=0.5 Enter";
                String runCommand = imageData.gh_fileName + " " + commParams + " Enter";
                foreach (String value in stringValues)
                {
                    runCommand += " " + value + " Enter";
                }
                rhino_wrapper.rhino_app.RunScript(runCommand, 1);
            }
            catch (Exception e)
            {
                log("Exception in Run_Script_And_Render(). e.Message=" + e.Message);
                return false;
            }


            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            log("Script ran After " + fromStart + " milliseconds");

            return true;

        }

        private static bool Set_GH_Params(ImageDataRequest imageData)
        {
            log("Starting Set_Params_And_Render(ImageData imageData=" + imageData.ToString() + ")");
            DateTime beforeTime = DateTime.Now;
            String logLine;
            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);

            foreach (String paramName in imageData.propValues.Keys)
            {
                Object value = imageData.propValues[paramName];
                if (!rhino_wrapper.grasshopper.AssignDataToParameter(paramName, value))
                {
                    fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                    logLine = "grasshopper.AssignDataToParameter(paramName=" + paramName + ", value=" + value + ") returned false After " + fromStart + " milliseconds";
                    log(logLine);
                    return false;
                }

                fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                logLine = "After assigning param:" + paramName + " the value=" + value + " After " + fromStart + " milliseconds";
                log(logLine);

            }

            rhino_wrapper.grasshopper.RunSolver(true);

            Object objRes = rhino_wrapper.grasshopper.BakeDataInObject(imageData.bake);

            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "After baking object:" + imageData.bake + " After " + fromStart + " milliseconds";
            log(logLine);

            return true;

        }

        private static bool setDefaultLayer(string layerName)
        {
            String setLayerCommand = "_EZ3DSilentChangeLayerCommand " + layerName;
            int setLayerCommandRes = rhino_wrapper.rhino_app.RunScript(setLayerCommand, 1);
            return true;
        }

        private static bool DeleteAll()
        {
            DateTime beforeTime = DateTime.Now;
            String logLine;
            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "Starting DeleteAll()";
            log(logLine);


            // Delete all
            String deleteAllCommand = "EZ3DDellAllCommand";
            int deleteAllCommanddRes = rhino_wrapper.rhino_app.RunScript(deleteAllCommand, 1);
            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "deleteAllCommanddRes=" + deleteAllCommanddRes + " After " + fromStart + " milliseconds";
            log(logLine);
            return true;

        }


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


    }
}
