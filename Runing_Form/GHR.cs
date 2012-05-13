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
        public int item_id;
        public Dictionary<String, Double> propValues = new Dictionary<string, double>();
        public String gh_fileName;
        public String scene;
        public string operation;

        public override string ToString()
        {
            String res = "item_id=" + item_id.ToString() + Environment.NewLine;
            res += "gh_fileName=" + gh_fileName + Environment.NewLine;
            res += "rhino_fileName=" + scene + Environment.NewLine;
            res += "bake=" + bake + Environment.NewLine;
            res += "params:" + Environment.NewLine;
            foreach (String key in propValues.Keys)
            {
                res += "    " + key + "=" + propValues[key].ToString() + Environment.NewLine;
            }
            return res;
        }
    }

    public class GHR
    {
        public Rhino5Application rhino;
        public dynamic grasshopper;
        public int id = -1;
        public String current_GH_file = null;
        public String current_Rhino_File = null;

        public static String Python_Scripts_Actual_Folder_Path = null;


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
            else imageData.item_id = (int)jsonDict["item_id"];

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

            if (!jsonDict.ContainsKey("params"))
            {
                Console.WriteLine("ERROR !!! - (!jsonDict.ContainsKey(\"params\"))");
                return false;
            }
            else
            {

                Dictionary<String, Double> paramValues = new Dictionary<string, double>();
                Dictionary<String, Object> paramObjects = (Dictionary<String, Object>)jsonDict["params"];
                foreach (String key in paramObjects.Keys)
                {
                    Object obj = paramObjects[key];
                    if (obj.GetType() == typeof(Decimal))
                    {
                        Decimal dec = (Decimal)paramObjects[key];
                        paramValues[key] = Decimal.ToDouble(dec);
                    }
                    else if (obj.GetType() == typeof(int))
                    {
                        int _int = (int)paramObjects[key];
                        paramValues[key] = (Double)_int;
                    }
                }
                imageData.propValues = paramValues;
            }
            return true;
        }


        public GHR(int id, Rhino5Application rhino, dynamic grasshopper)
        {
            this.id = id;
            this.grasshopper = grasshopper;
            this.rhino = rhino;

        }


        int get_msg_failures = 0;
        int get_msg_failures_allowed = 3;
        private bool single_cycle()
        {
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
                    return true;
                }
                MyLog("Get_Msg_From_Req_Q() failed and (" + get_msg_failures + "=get_msg_failures >= get_msg_failures_allowed=" + get_msg_failures_allowed + ") !!!");
                return false;
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
                return true;
            }

            Utils.lastMsg_Time = DateTime.Now;

            // Extract the ImageData
            ImageDataRequest imageData = null;
            if (!deciferImageDataFromBody(msg.Body, out imageData))
            {
                MyLog("GHR_Dispatcher.deciferImagesDataFromJSON(msg.Body=" + msg.Body + ", out imagesDatas) failed!!!");
                return false;
            }

            // Add Msg to Queue_Readies
            if (!SQS.Send_Msg_To_Readies_Q(RenderStatus.STARTED, imageData.item_id, beforeProcessingTime))
            {
                MyLog("Form1.Send_Msg_To_Readies_Q(status=STARTED,imageData.item_id=" + imageData.item_id + ") failed");
                return false;
            }


            if (imageData.operation == "render_model")
            {
                // Process Msg to picture
                String imageFilePath = Runing_Form.images_DirPath + Path.DirectorySeparatorChar + "yofi_" + imageData.item_id + ".jpg";
                if (!Process_Into_Image_File(imageData, imageFilePath))
                {
                    MyLog("Process_Msg_Into_Image_File(msg) failed!!!");
                    // Add Msg to Queue_Readies
                    if (!SQS.Send_Msg_To_Readies_Q(RenderStatus.ERROR, imageData.item_id, beforeProcessingTime))
                    {
                        MyLog("Form1.Send_Msg_To_Readies_Q(status=ERROR,imageData.item_id=" + imageData.item_id + ") failed");
                    }
                    return false;
                }

                // Delete Msg From Queue_Requests
                if (!SQS.Delete_Msg_From_Req_Q(msg))
                {
                    MyLog("Delete_Msg_From_Req_Q(msg) failed!!!");
                    // Add Msg to Queue_Readies
                    if (!SQS.Send_Msg_To_Readies_Q(RenderStatus.ERROR, imageData.item_id, beforeProcessingTime))
                    {
                        MyLog("Form1.Send_Msg_To_Readies_Q(status=ERROR,imageData.item_id=" + imageData.item_id + ") failed");
                    }
                    return false;
                }
                // Add Msg to Queue_Readies
                if (!SQS.Send_Msg_To_Readies_Q(RenderStatus.FINISHED, imageData.item_id, beforeProcessingTime))
                {
                    MyLog("Form1.Send_Msg_To_Readies_Q(imageData.item_id=" + imageData.item_id + ") failed");
                    return false;
                }
                return true;
            }
            else
            {
                MyLog("ERROR !!! - (" + imageData.operation + "=imageData.operation != \"render_model\")");
            }
            MyLog("imageData=" + imageData.ToString());
            return false;
        }

        public void new_runner()
        {

            while (true)
            {
                if (!single_cycle())
                {
                    MyLog("single_cycle() failed!!!");
                    return;
                }
            }
        }




        private bool Process_Into_Image_File(ImageDataRequest imageData, String resultingImagePath)
        {
            DateTime beforeTime = DateTime.Now;

            String logLine = "Starting Get_Pictures()";
            MyLog(logLine);

            if (imageData.scene != null)
            {
                if (imageData.scene != current_Rhino_File)
                {
                    String sceneFilePath = Runing_Form.scenes_DirPath + Path.DirectorySeparatorChar + imageData.scene;
                    String replicateFilePath = Runing_Form.scenes_DirPath + Path.DirectorySeparatorChar + "rep_" + id +"_"+ imageData.scene;
                    File.Copy(sceneFilePath, replicateFilePath, true);
                    try
                    {
                        String saveCommand = "-Save N";
                        int saveCommandRes = rhino.RunScript(saveCommand, 1);
                    }
                    catch (Exception e)
                    {
                    }

                    Console.WriteLine("Loading scene Rhino # " + id + " at " + DateTime.Now);
                    String openCommand = "-Open " + replicateFilePath;
                    int openCommandRes = rhino.RunScript(openCommand, 1);

                    int isInitialized = rhino.IsInitialized();
                    if (isInitialized != 1)
                    {
                        MyLog("ERROR!!: " + isInitialized + "==isInitialized != 1)");
                        return false;
                    }

                    current_Rhino_File = imageData.scene;
                }
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


                if (!Set_Params_And_Render(imageData, resultingImagePath))
                {
                    logLine = "Set_Params_And_Render(imageData=" + imageData.ToString() + "], filePath=" + resultingImagePath + ") failed !!!";
                    MyLog(logLine);
                    return false;
                }
            }
            else
            {
                Run_Script_And_Render(imageData, resultingImagePath);
            }

            String fileName_on_S3 = imageData.item_id.ToString() + ".jpg";
            if (!S3.Write_File_To_S3(resultingImagePath, fileName_on_S3))
            {
                logLine = "Write_File_To_S3(resultingImagePath=" + resultingImagePath + ", fileName_on_S3=" + fileName_on_S3 + ") failed !!!";
                MyLog(logLine);
                return false;
            }


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
                grasshopper.CloseAllDocuments();
                Thread.Sleep(1000);
                grasshopper.OpenDocument(filePath);
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
            String selectAllCommand = "SelLayerNumber 0";
            int selectCommandRes = rhino.RunScript(selectAllCommand, 1);
            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "selectCommandRes=" + selectCommandRes + " After " + fromStart + " milliseconds";
            MyLog(logLine);


            String deleteCommand = "Delete";
            int deleteCommandRes = rhino.RunScript(deleteCommand, 1);
            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "deleteCommandRes=" + selectCommandRes + " After " + fromStart + " milliseconds";
            MyLog(logLine);
            return true;

        }
        public bool Run_Script_And_Render(ImageDataRequest imageData, String outputPath)
        {
            DeleteAll();
            String commParams = "";
            foreach (String paramName in imageData.propValues.Keys)
            {
                Double value = imageData.propValues[paramName];
                commParams = commParams + " " + paramName + "=" + value;
            }

            //String runCommand = "vase1 rad1=0.2 rad2=0.42 rad3=0.6 rad4=0.5 Enter";
            String runCommand = imageData.gh_fileName + " " + commParams + " Enter";
            rhino.RunScript(runCommand, 1);

            String captureCommand = "-FlamingoRenderTo f " + outputPath + " " + 180 + " " + 180;
            int captureCommandRes = rhino.RunScript(captureCommand, 1);

            return true;
        }
        public bool Set_Params_And_Render(ImageDataRequest imageData, String outputPath)
        {
            MyLog("Starting Save_A_Picture(ImageData imageData, String outputPath))");
            DateTime beforeTime = DateTime.Now;
            String logLine;
            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);

            DeleteAll();


            foreach (String paramName in imageData.propValues.Keys)
            {
                Double value = imageData.propValues[paramName];
                if (!grasshopper.AssignDataToParameter(paramName, value))
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

            grasshopper.RunSolver(true);

            Object objRes = grasshopper.BakeDataInObject(imageData.bake);

            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "After baking object:" + imageData.bake + " After " + fromStart + " milliseconds";
            //MyLog(logLine);

            String captureCommand = "-FlamingoRenderTo f " + outputPath + " " + 180 + " " + 180;
            int captureCommandRes = rhino.RunScript(captureCommand, 1);
            MyLog("Image rendered by:" + captureCommand);

            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "After rendering into" + outputPath + " After " + fromStart + " milliseconds";
            MyLog(logLine);
            return true;
        }



    }

    public enum THREAD_RESPONSE
    {
        NO_RESPONSE,
        SUCCESS,
        FAIL
    }



}
