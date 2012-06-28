using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Rhino4;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Net;
using System.Windows.Forms;
using System.Web.Mail;

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Web.Script.Serialization;


namespace Runing_Form
{
    public enum RenderStatus
    {
        STARTED,
        FINISHED,
        ERROR
    }
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

    public class Rhino_Wrapper
    {
        public Rhino5Application rhino_app;
        public int rhino_pid;
        public DateTime creationTime;
        public DateTime killTime;
        public dynamic grasshopper;
    }

    public class GHR
    {
        public Rhino_Wrapper rhino_wrapper;
        public int id = -1;
        public String current_GH_file = null;
        public String current_Rhino_File = null;

        public static String Python_Scripts_Actual_Folder_Path = null;

        public static Queue<Rhino_Wrapper> spareRhinos = new Queue<Rhino_Wrapper>();
        public static Queue<int> rhino_requestors_Q = new Queue<int>();
        //public static Dictionary<
        public static Type locker = typeof(GHR);
        public static Type requestors_locker = typeof(GHR);
        public static bool getRhinoFromQueue(out Rhino_Wrapper outRhino)
        {
            outRhino = null;
            lock (locker)
            {
                if (spareRhinos.Count == 0) return false;
                outRhino = spareRhinos.Dequeue();
                return true;
            }
        }

        public static bool pushRhinoIntoQueue(Rhino_Wrapper newRhino)
        {
            lock (locker)
            {
                spareRhinos.Enqueue(newRhino);
            }
            return true;
        }

        public static bool deciferImageDataFromBody(String msgBody, out ImageDataRequest imageData)
        {
            imageData = new ImageDataRequest();

            String jsonString = Utils.DecodeFrom64(msgBody);

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

        public THREAD_RESPONSE last_Response = THREAD_RESPONSE.NO_RESPONSE;
        public String last_request_id;
        public Dictionary<String, bool> shittyIDs = new Dictionary<string, bool>();
        public Amazon.SQS.Model.Message last_msg;

        public GHR(int id, Rhino_Wrapper rhino)
        {
            this.id = id;
            this.rhino_wrapper = rhino;

        }


        int get_msg_failures = 0;
        int get_msg_failures_allowed = 3;
        private void single_cycle()
        {
            DateTime beforeTime = DateTime.Now;
            last_Response = THREAD_RESPONSE.NO_RESPONSE;
            last_request_id = String.Empty;
            // Get a single MSG from Queue_Requests
            bool msgFound;
            Amazon.SQS.Model.Message msg;
            if (!SQS.Get_Msg_From_Req_Q(out msg, out msgFound))
            {
                MyLog("Get_Msg_From_Req_Q() failed (# " + get_msg_failures + ") !!!");
                if (get_msg_failures < get_msg_failures_allowed)
                {
                    get_msg_failures++;
                    Thread.Sleep(1000);
                    last_Response = THREAD_RESPONSE.FAIL;
                    return;
                }
                MyLog("Get_Msg_From_Req_Q() failed and (" + get_msg_failures + "=get_msg_failures >= get_msg_failures_allowed=" + get_msg_failures_allowed + ") !!!");
                last_Response = THREAD_RESPONSE.FAIL;
                return;
            }
            if (get_msg_failures > 0)
            {
                MyLog("Get_Msg_From_Req_Q() succeeded after " + get_msg_failures + " failures");
            }

            DateTime beforeProcessingTime = DateTime.Now;
            get_msg_failures = 0;

            // if there is No Msg - Sleep & continue;
            if (!msgFound)
            {
                Thread.Sleep(250);
                last_Response = THREAD_RESPONSE.NO_REQUEST;
                return;
            }

            Utils.lastMsg_Time = DateTime.Now;

            // Extract the ImageData
            ImageDataRequest imageData = null;
            if (!deciferImageDataFromBody(msg.Body, out imageData))
            {
                MyLog("GHR_Dispatcher.deciferImagesDataFromJSON(msg.Body=" + msg.Body + ", out imagesDatas) failed!!!");
                last_Response = THREAD_RESPONSE.FAIL;
                return;
            }

            last_request_id = imageData.item_id;
            last_msg = msg;

            // Add Msg to Queue_Readies
            if (!SQS.Send_Msg_To_Readies_Q(RenderStatus.STARTED, imageData.item_id, beforeProcessingTime))
            {
                MyLog("Form1.Send_Msg_To_Readies_Q(status=STARTED,imageData.item_id=" + imageData.item_id + ") failed");
                last_Response = THREAD_RESPONSE.FAIL;
                return;
            }


            if (imageData.operation == "render_model")
            {
                DateTime beforeRhino = DateTime.Now;
                // Process Msg to picture
                String resultingLocalImageFilePath;
                TimeSpan renderTime, buildTime, waitTime;
                if (!Process_Into_Image_File(imageData, out resultingLocalImageFilePath, out renderTime, out buildTime, out waitTime))
                {
                    String logLine = "Process_Msg_Into_Image_File(msg) failed!!! (). ImageData=" + imageData.ToString();
                    MyLog(logLine);
                    SQS.Send_Msg_To_ERROR_Q(id, logLine);
                    last_Response = THREAD_RESPONSE.FAIL;
                    return;
                }

                DateTime afterRhino_Before_S3 = DateTime.Now;

                String fileName_on_S3 = imageData.item_id.ToString() + ".jpg";
                if (!S3.Write_File_To_S3(resultingLocalImageFilePath, fileName_on_S3))
                {
                    String logLine = "Write_File_To_S3(resultingImagePath=" + resultingLocalImageFilePath + ", fileName_on_S3=" + fileName_on_S3 + ") failed !!!";
                    SQS.Send_Msg_To_ERROR_Q(id, logLine);
                    last_Response = THREAD_RESPONSE.FAIL;
                    return;
                }

                DateTime afterS3_Before_SQS = DateTime.Now;

                // Delete Msg From Queue_Requests
                if (!SQS.Delete_Msg_From_Req_Q(msg))
                {
                    MyLog("Delete_Msg_From_Req_Q(msg) failed!!!");
                    // Add Msg to Queue_Readies
                    if (!SQS.Send_Msg_To_Readies_Q(RenderStatus.ERROR, imageData.item_id, beforeProcessingTime))
                    {
                        MyLog("Form1.Send_Msg_To_Readies_Q(status=ERROR,imageData.item_id=" + imageData.item_id + ") failed");
                    }
                    last_Response = THREAD_RESPONSE.FAIL;
                    return;
                }
                // Add Msg to Queue_Readies
                if (!SQS.Send_Msg_To_Readies_Q(RenderStatus.FINISHED, imageData.item_id, beforeProcessingTime))
                {
                    MyLog("Form1.Send_Msg_To_Readies_Q(imageData.item_id=" + imageData.item_id + ") failed");
                    last_Response = THREAD_RESPONSE.FAIL;
                    return;
                }

                DateTime afterSQS = DateTime.Now;

                MyLog("Reading msg time =" + (beforeRhino - beforeProcessingTime).TotalMilliseconds.ToString() + " millis");
                MyLog("Wait time=" + waitTime.TotalMilliseconds.ToString() + " millis");
                MyLog("Rhino build time=" + buildTime.TotalMilliseconds.ToString() + " millis");
                MyLog("Render time=" + renderTime.TotalMilliseconds.ToString() + " millis");
                MyLog("S3 Time=" + (afterS3_Before_SQS - afterRhino_Before_S3).TotalMilliseconds.ToString() + " millis");
                MyLog("SQS Time=" + (afterSQS - afterS3_Before_SQS).TotalMilliseconds.ToString() + " millis");
                MyLog("Total single_cycle(id=" + imageData.item_id + ")=" + (afterSQS - beforeTime).TotalMilliseconds.ToString() + " millis");
        
                last_Response = THREAD_RESPONSE.SUCCESS;
                return;
            }
            else
            {
                MyLog("ERROR !!! - (" + imageData.operation + "=imageData.operation != \"render_model\")");
            }
            MyLog("imageData=" + imageData.ToString());
            last_Response = THREAD_RESPONSE.FAIL;
            return;
        }
  

        public void new_runner()
        {

            while (true)
            {
                Thread thread = new Thread(new ThreadStart(this.single_cycle));
                thread.Start();

                if (!thread.Join(15000))
                {
                    MyLog("single cycle 2 has timed out !!!");
                    if (last_request_id != String.Empty)
                    {
                        if (shittyIDs.ContainsKey(last_request_id))
                        {
                            // this is a second timeout for this msg - remove it from the AWS SQS + send an error msg
                            SQS.Delete_Msg_From_Req_Q(last_msg);
                            String errorMsg = "Second timeout on request with id=" + last_request_id + ". Removing from Q";
                            SQS.Send_Msg_To_ERROR_Q(id, errorMsg);
                        }
                        else
                        {
                            shittyIDs[last_request_id] = true;
                        }
                    }

                    // Close previous rhino
                    try
                    {
                        brutally_killPrivRhino();
                    }
                    catch (Exception e)
                    {
                        MyLog("Exception in Process_Into_Image_File(1). e.Message=" + e.Message);
                        return;
                    }

                    if (!waitForRhinoFromQueue(out rhino_wrapper, 150, 100))
                    {
                        MyLog("waitForRhinoFromQueue() failed!!!");
                        return;
                    }
                    current_Rhino_File = null;
                    current_GH_file = null;
                }
                else
                {
                    if (last_Response != THREAD_RESPONSE.NO_REQUEST)
                    {
                        MyLog("single cycle 2 exitted on time with lastSucceeded=" + last_Response.ToString());
                    }
                }

            }
        }


        private bool waitForRhinoFromQueue(out Rhino_Wrapper rhinoFromQueue, int tries, int delayEachTry)
        {
            rhinoFromQueue = null;
            MyLog("GHr id=" + id + " requesting new Rhino (before lock). Before rhino_requestors_Q.count=" + rhino_requestors_Q.Count);
            lock (GHR.requestors_locker)
            {
                MyLog("GHr id=" + id + " requesting new Rhino. Before rhino_requestors_Q.count=" + rhino_requestors_Q.Count);
                rhino_requestors_Q.Enqueue(id);
            }
            for (int i = 0; i < tries; i++)
            {
                lock (GHR.requestors_locker)
                {
                    if (rhino_requestors_Q.Peek() == id)
                    {
                        if (GHR.getRhinoFromQueue(out rhinoFromQueue))
                        {
                            rhino_requestors_Q.Dequeue();
                            MyLog("GHr id=" + id + " received new Rhino. After rhino_requestors_Q.count=" + rhino_requestors_Q.Count);
                            return true;
                        }
                    }
                }
                Thread.Sleep(delayEachTry);
            }
            return false;
        }

        private void brutally_killPrivRhino()
        {
            Thread killer_thread = new Thread(new ThreadStart(this.closePrivRhino));
            killer_thread.Start();
            killer_thread.Join(5000);

            Process p = Process.GetProcessById(rhino_wrapper.rhino_pid);
            if (p != null) p.Kill();
        }

        private void closePrivRhino()
        {
            try
            {
                MyLog("Save & Exit for rhino file=" + current_Rhino_File);

                try
                {
                    if (rhino_wrapper.grasshopper != null) rhino_wrapper.grasshopper.CloseAllDocuments();
                }
                catch (Exception e_Stam)
                {
                    MyLog("Exception e_Stam Process_Into_Image_File(0). e_Stam.Message=" + e_Stam.Message);
                }


                String exitCommand = "-Exit";
                int exitCommandRes = rhino_wrapper.rhino_app.RunScript(exitCommand, 1);
            }
            catch (Exception e)
            {
                MyLog("Exception in Process_Into_Image_File(1). e.Message=" + e.Message);
            }

            return;
        }

        private bool Process_Into_Image_File(ImageDataRequest imageData, out String localImageFilePath, out TimeSpan renderTime, out TimeSpan buildTime, out TimeSpan waitTime)
        {
            DateTime beforeTime = DateTime.Now;

            String logLine = "Starting Get_Pictures()";
            MyLog(logLine);
            localImageFilePath = String.Empty;
            renderTime = new TimeSpan();
            buildTime = new TimeSpan();
            waitTime = new TimeSpan();

            if ((current_Rhino_File != null) && (imageData.scene != current_Rhino_File))
            {
                // Close previous rhino
                try
                {
                    brutally_killPrivRhino();
                }
                catch (Exception e)
                {
                    MyLog("Exception in Process_Into_Image_File(1). e.Message=" + e.Message);
                    return false;
                }

                DateTime beforeWait = DateTime.Now;

                if (!waitForRhinoFromQueue(out rhino_wrapper, 150, 100))
                {
                    MyLog("waitForRhinoFromQueue() failed!!!");
                    return false;
                }

                waitTime = DateTime.Now - beforeWait;

                current_Rhino_File = null;
                current_GH_file = null;
            }
            if (imageData.scene != null)
            {
                if (imageData.scene != current_Rhino_File)
                {
                    String sceneFilePath = Runing_Form.scenes_DirPath + Path.DirectorySeparatorChar + imageData.scene;
                    if (!File.Exists(sceneFilePath))
                    {
                        return false;
                    }

                    Console.WriteLine("Loading scene Rhino # " + id + " at " + DateTime.Now);
                    String openCommand = "-Open " + sceneFilePath;
                    int openCommandRes = rhino_wrapper.rhino_app.RunScript(openCommand, 1);

                    int isInitialized = rhino_wrapper.rhino_app.IsInitialized();
                    if (isInitialized != 1)
                    {
                        MyLog("ERROR!!: " + isInitialized + "==isInitialized != 1)");
                        return false;
                    }

                    current_Rhino_File = imageData.scene;
                }
            }

            if (!DeleteAll())
            {
                MyLog("ERROR!!: DeleteAll() failed !!!");
                return false;
            }

            if (!setDefaultLayer(imageData.layerName))
            {
                MyLog("ERROR!!: setDefaultLayer(layerName=" + imageData.layerName + ") failed !!!");
                return false;
            }

            if (imageData.gh_fileName.EndsWith(".gh") || imageData.gh_fileName.EndsWith(".ghx"))
            {


                if (current_GH_file == imageData.gh_fileName)
                {
                    logLine = "Skipping Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_fileName + ")";
                    MyLog(logLine);
                }
                else
                {
                    if (!Open_GH_File(Runing_Form.GH_DirPath + Path.DirectorySeparatorChar + imageData.gh_fileName))
                    {
                        logLine = "Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_fileName + "); failed";
                        MyLog(logLine);
                        return false;
                    }
                    current_GH_file = imageData.gh_fileName;
                }


                if (!Set_GH_Params(imageData))
                {
                    logLine = "Set_Params(imageData=" + imageData.ToString() + "]) failed !!!";
                    MyLog(logLine);
                    return false;
                }
            }
            else
            {
                if (!Run_Script(imageData))
                {
                    logLine = "Run_Script(imageData=" + imageData.ToString() + "]) failed !!!";
                    MyLog(logLine);
                    return false;
                }
            }


            buildTime = DateTime.Now - beforeTime;


            String resultingImagePath;
            if (!Render(imageData, out resultingImagePath))
            {
                MyLog("Render(imageData=" + imageData.ToString() + ") failed !!!");
                return false;
            }

            renderTime = (DateTime.Now - beforeTime) - buildTime;
            localImageFilePath = resultingImagePath;

            DateTime afterTime = DateTime.Now;
            int timed = (int)((afterTime - beforeTime).TotalMilliseconds);
            MyLog("Total Get_Pictures() call took " + timed + " millseconds");
            return true;

        }




        public bool Open_GH_File(String filePath)
        {
            MyLog("Starting  Open_GH_File(*,filePath=" + filePath);
            DateTime before = DateTime.Now;

            try
            {
                rhino_wrapper.grasshopper.CloseAllDocuments();
                Thread.Sleep(1000);
                rhino_wrapper.grasshopper.OpenDocument(filePath);
            }
            catch (Exception e)
            {
                MyLog("Exception=" + e.Message);
                return false;
            }

            MyLog("Finished succefully  Open_GH_File(*,filePath=" + filePath + ((int)(DateTime.Now - before).TotalMilliseconds) + " miliseconds after Starting");
            return true;
        }

        public void MyLog(String line)
        {
            DateTime now = DateTime.Now;
            Console.WriteLine("(id=" + id + ")(" + now.Hour + ":" + now.Minute + ":" + now.Second + "." + now.Millisecond + ") " + line);
        }


        public bool DeleteAll()
        {
            DateTime beforeTime = DateTime.Now;
            String logLine;
            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "Starting DeleteAll()";
            MyLog(logLine);


            // Delete all
            String deleteAllCommand = "EZ3DDellAllCommand";
            int deleteAllCommanddRes = rhino_wrapper.rhino_app.RunScript(deleteAllCommand, 1);
            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "deleteAllCommanddRes=" + deleteAllCommanddRes + " After " + fromStart + " milliseconds";
            MyLog(logLine);
            return true;

        }


        public bool setDefaultLayer(String layerName)
        {
            String setLayerCommand = "_EZ3DSilentChangeLayerCommand " + layerName;
            int setLayerCommandRes = rhino_wrapper.rhino_app.RunScript(setLayerCommand, 1);
            return true;
        }
        public bool Run_Script(ImageDataRequest imageData)
        {
            MyLog("Starting Run_Script_And_Render(ImageData imageData=" + imageData.ToString() + "))");
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
                MyLog("Exception in Run_Script_And_Render(). e.Message=" + e.Message);
                return false;
            }


            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            MyLog("Script ran After " + fromStart + " milliseconds");

            return true;
        }

        private bool Render(ImageDataRequest imageData, out String outputPath)
        {
            outputPath = Runing_Form.images_DirPath + Path.DirectorySeparatorChar + "yofi_" + imageData.item_id + ".jpg";
            try
            {

                DateTime beforeTime = DateTime.Now;
                String captureCommand = "-FlamingoRenderTo f " + outputPath + " " + imageData.imageSize.Width + " " + imageData.imageSize.Height;
                int captureCommandRes = rhino_wrapper.rhino_app.RunScript(captureCommand, 1);

                int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                MyLog("After rendering by: " + captureCommand + "  into" + outputPath + " After " + fromStart + " milliseconds");

            }
            catch (Exception e)
            {
                MyLog("Excpetion in Render(imageData=" + imageData.ToString() + ", String outputPath=" + outputPath + ", e.Message=" + e.Message);
                return false;
            }

            return true;
        }
        public bool Set_GH_Params(ImageDataRequest imageData)
        {
            MyLog("Starting Set_Params_And_Render(ImageData imageData=" + imageData.ToString() + ")");
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
                    MyLog(logLine);
                    return false;
                }

                fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                logLine = "After assigning param:" + paramName + " the value=" + value + " After " + fromStart + " milliseconds";
                MyLog(logLine);

            }

            rhino_wrapper.grasshopper.RunSolver(true);

            Object objRes = rhino_wrapper.grasshopper.BakeDataInObject(imageData.bake);

            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "After baking object:" + imageData.bake + " After " + fromStart + " milliseconds";
            MyLog(logLine);

            return true;
        }

        public static bool startSingleRhino(bool visible, out Rhino_Wrapper newRhino)
        {
            newRhino = new Rhino_Wrapper();
            lock (GHR.locker)
            {
                Process[] procs_before = Process.GetProcessesByName("Rhino4");
                Console.WriteLine("Starting Rhino at " + DateTime.Now);
                newRhino.rhino_app = new Rhino5Application();
                newRhino.rhino_app.ReleaseWithoutClosing = 1;
                newRhino.rhino_app.Visible = visible ? 1 : 0;
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



    }

    public enum THREAD_RESPONSE
    {
        NO_RESPONSE,
        SUCCESS,
        NO_REQUEST,
        FAIL
    }



}
