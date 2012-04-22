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
       public String gh_filePath;
       public string operation;

       public override string ToString()
       {
           String res = "item_id=" + item_id.ToString() + Environment.NewLine;
           res += "gh_filePath=" + gh_filePath + Environment.NewLine;
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

        // The following veriables are for SQS communications
        public static AmazonSQS sqs;
        public static String requests_Q_url = null;
        public static String ready_Q_url = null;

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
            else imageData.gh_filePath = (String)jsonDict["gh_file"];

            if (!jsonDict.ContainsKey("item_id"))
            {
                Console.WriteLine("ERROR !!! - (!jsonDict.ContainsKey(\"item_id\"))");
                return false;
            }
            else imageData.item_id = (int)jsonDict["item_id"];

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
                Dictionary<String, Object> paramObjects = (Dictionary<String,Object>)jsonDict["params"];
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

        private bool Get_Msg_From_Req_Q(out Amazon.SQS.Model.Message msg, out bool msgFound)
        {
            msgFound = false;
            msg = null;
            try
            {
                ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
                receiveMessageRequest.MaxNumberOfMessages = 1;
                receiveMessageRequest.QueueUrl = requests_Q_url;
                ReceiveMessageResponse receiveMessageResponse = sqs.ReceiveMessage(receiveMessageRequest);
                if (receiveMessageResponse.IsSetReceiveMessageResult())
                {
                    ReceiveMessageResult receiveMessageResult = receiveMessageResponse.ReceiveMessageResult;
                    List<Amazon.SQS.Model.Message> receivedMsges = receiveMessageResponse.ReceiveMessageResult.Message;
                    if (receivedMsges.Count == 0)
                    {
                        return true;
                    }
                    msgFound = true;
                    msg = receivedMsges[0];
                }

            }
            catch (AmazonSQSException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
                Console.WriteLine("XML: " + ex.XML);
                return false;
            }
            return true;

        }

        int get_msg_failures = 0;
        int get_msg_failures_allowed = 3;
        private bool single_cycle()
        {
            // Get a single MSG from Queue_Requests
            bool msgFound;
            Amazon.SQS.Model.Message msg;
            if (!Get_Msg_From_Req_Q(out msg, out msgFound))
            {
                MyLog("Get_Msg_From_Req_Q() failed (# " + get_msg_failures +") !!!");
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

            // Extract the ImageData
            ImageDataRequest imageData = null;
            if (!deciferImageDataFromBody(msg.Body, out imageData))
            {
                MyLog("GHR_Dispatcher.deciferImagesDataFromJSON(msg.Body=" + msg.Body + ", out imagesDatas) failed!!!");
                return false;
            }

            // Add Msg to Queue_Readies
            if (!Send_Msg_To_Readies_Q(RenderStatus.STARTED, imageData.item_id, beforeProcessingTime))
            {
                MyLog("Form1.Send_Msg_To_Readies_Q(status=STARTED,imageData.item_id=" + imageData.item_id + ") failed");
                return false;
            }


            if (imageData.operation == "render_model")
            {
                // Process Msg to picture
                String imageFilePath = Form1.images_DirPath + Path.DirectorySeparatorChar + "yofi_" + imageData.item_id + ".jpg";
                if (!Process_Into_Image_File(imageData, imageFilePath))
                {
                    MyLog("Process_Msg_Into_Image_File(msg) failed!!!");
                    // Add Msg to Queue_Readies
                    if (!Send_Msg_To_Readies_Q(RenderStatus.ERROR, imageData.item_id, beforeProcessingTime))
                    {
                        MyLog("Form1.Send_Msg_To_Readies_Q(status=ERROR,imageData.item_id=" + imageData.item_id + ") failed");
                    }
                    return false;
                }

                // Delete Msg From Queue_Requests
                if (!Delete_Msg_From_Req_Q(msg))
                {
                    MyLog("Delete_Msg_From_Req_Q(msg) failed!!!");
                    // Add Msg to Queue_Readies
                    if (!Send_Msg_To_Readies_Q(RenderStatus.ERROR, imageData.item_id, beforeProcessingTime))
                    {
                        MyLog("Form1.Send_Msg_To_Readies_Q(status=ERROR,imageData.item_id=" + imageData.item_id + ") failed");
                    }
                    return false;
                }
                // Add Msg to Queue_Readies
                if (!Send_Msg_To_Readies_Q(RenderStatus.FINISHED,imageData.item_id, beforeProcessingTime))
                {
                    MyLog("Form1.Send_Msg_To_Readies_Q(imageData.item_id=" + imageData.item_id + ") failed");
                    return false;
                }
                return true;
            }
            else
            {
                MyLog("ERROR !!! - (" + imageData.operation + "=imageData.operation != \"render_model\")");            }
                MyLog("imageData="+imageData.ToString());
                return false;
        }

        public void new_runner()
        {
            if (!Initialize_SQS_stuff())
            {
                MyLog("Initialize_SQS_stuff() failed!!!");
                return;
            }

            while (true)
            {
                if (!single_cycle())
                {
                    MyLog("single_cycle() failed!!!");
                    return;
                }
            }
        }

        public bool Send_Msg_To_Readies_Q(RenderStatus status,int item_id, DateTime beforeProcessingTime)
        {
            try
            {

                Dictionary<string, object> dict = new Dictionary<string, object>();

                DateTime current = DateTime.Now;
                TimeSpan duration = current - beforeProcessingTime;

                dict["item_id"]=item_id;
                dict["url"]=@"http://" + Form1.my_ip + @"/testim/yofi_" + item_id + ".jpg";
                dict["duration"] = Math.Round(duration.TotalSeconds,3);
                dict["status"] = status.ToString();

                JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                string jsonString = serializer.Serialize((object)dict);

                SendMessageRequest sendMessageRequest = new SendMessageRequest();
                sendMessageRequest.QueueUrl = ready_Q_url; //URL from initial queue creation
                sendMessageRequest.MessageBody = Utils.EncodeTo64(jsonString);

                MyLog("Before sending ready msg(" + sendMessageRequest.MessageBody + ").");
                sqs.SendMessage(sendMessageRequest);
                MyLog("After sending ready msg(" + sendMessageRequest.MessageBody + ").");
            }
            catch (AmazonSQSException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
                Console.WriteLine("XML: " + ex.XML);
                return false;
            }
            return true;
        }

        public bool Delete_Msg_From_Req_Q(Amazon.SQS.Model.Message msg)
        {
            try
            {
                String messageRecieptHandle = msg.ReceiptHandle;

                //Deleting a message
                MyLog("Deleting the message.\n");
                DeleteMessageRequest deleteRequest = new DeleteMessageRequest();
                deleteRequest.QueueUrl = requests_Q_url;
                deleteRequest.ReceiptHandle = messageRecieptHandle;
                MyLog("Before deleting incoming msg(" + messageRecieptHandle + ").");
                sqs.DeleteMessage(deleteRequest);
                MyLog("After deleting incoming msgs().");

            }
            catch (AmazonSQSException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
                Console.WriteLine("XML: " + ex.XML);
                return false;
            }
            return true;
        }


        private bool Process_Into_Image_File(ImageDataRequest imageData,String resultingImagePath)
        {
            DateTime beforeTime = DateTime.Now;

            String logLine = "Starting Get_Pictures()";
            MyLog(logLine);


            if (current_GH_file == imageData.gh_filePath)
            {
                logLine = "Skipping Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_filePath + ")";
                MyLog(logLine);
            }
            else
            {
                if (!Open_GH_File(Form1.GH_DirPath + Path.DirectorySeparatorChar + imageData.gh_filePath))
                {
                    logLine = "Open_GH_File(imageData[imageData.gh_filePath=" + imageData.gh_filePath + "); failed";
                    MyLog(logLine);
                    return false;
                }
                current_GH_file = imageData.gh_filePath;
            }

            if (!Set_Params_And_Render(imageData, resultingImagePath))
            {
                logLine = "Get_A_Picture(imageData=" + imageData.ToString() + "], filePath=" + resultingImagePath + "); failed";
                MyLog(logLine);
                return false;
            }
/*
            // delete msg from requests Q
            List<Amazon.SQS.Model.Message> dummyList = new List<Amazon.SQS.Model.Message>();
            dummyList.Add(msg);
            if (!Form1.Delete_Msgs_From_Req_Q(dummyList))
            {
                logLine = "Form1.Delete_Msgs_From_Req_Q(dummyList) failed";
                MyLog(logLine);
                return false;
            }

            // put msg in ready Q
            String imagePathForFTP;
            FileInfo imageFileInfo = new FileInfo(filePath);
            //localhost/tempImageFiles/yofi_135.jpg
            imagePathForFTP = @"ftp://" + System.Net.Dns.GetHostName() + @"/tempImageFiles" + @"/" + "yofi_" + imageData.item_id + ".jpg";
            if (!Form1.Send_Msg_To_Readies_Q(imageData.item_id, imagePathForFTP))
            {
                logLine = "Form1.Send_Msg_To_Readies_Q(imageData.item_id=" + imageData.item_id + ",imagePathForFTP=" + imagePathForFTP + ") failed";
                MyLog(logLine);
                return false;
            }
*/
            

            DateTime afterTime = DateTime.Now;
            int timed = (int)((afterTime - beforeTime).TotalMilliseconds);
            MyLog("Total Get_Pictures() call took " + timed + " millseconds");
            return true;

        }

        private bool Initialize_SQS_stuff()
        {
            try
            {
                if (!Utils.CFG.ContainsKey("request_Q_name"))
                {
                    Console.WriteLine("param request_Q_name is not found in ez3d.config");
                    return false;
                }
                if (!Utils.CFG.ContainsKey("ready_Q_name"))
                {
                    Console.WriteLine("param ready_Q_name is not found in ez3d.config");
                    return false;
                }

                //sqs = AWSClientFactory.CreateAmazonSQSClient(new AmazonSQSConfig().WithServiceURL(@"http://sqs.eu-west-1.amazonaws.com"));
                sqs = AWSClientFactory.CreateAmazonSQSClient();

                ready_Q_url = requests_Q_url = null;
                ListQueuesRequest listQueuesRequest = new ListQueuesRequest();
                ListQueuesResponse listQueuesResponse = sqs.ListQueues(listQueuesRequest);
                if (listQueuesResponse.IsSetListQueuesResult())
                {
                    ListQueuesResult listQueuesResult = listQueuesResponse.ListQueuesResult;
                    foreach (String str in listQueuesResult.QueueUrl)
                    {
                        if (str.EndsWith('/' + Utils.CFG["request_Q_name"]))
                        {
                            requests_Q_url = str;
                            Console.WriteLine("requests_Q_url =" +requests_Q_url);
                        }
                        if (str.EndsWith('/' + Utils.CFG["ready_Q_name"]))
                        {
                            ready_Q_url = str;
                            Console.WriteLine("ready_Q_url =" + ready_Q_url);
                        }
                    }


                    if (requests_Q_url == null)
                    {
                        Console.WriteLine("(requests_Q_url == null)");
                        return false;
                    }
                    if (ready_Q_url == null)
                    {
                        Console.WriteLine("(ready_Q_url == null)");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("listQueuesResponse.IsSetListQueuesResult() == false");
                    return false;
                }

                Console.WriteLine("Get_Queues_URLs fininshed succefully");
                return true;
            }
            catch (Amazon.SQS.AmazonSQSException e)
            {
                Console.WriteLine("Amazon SQS caught !!!");
                Console.WriteLine(e.Message);
                return false;
            }

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

            MyLog("Finished succefully  Open_GH_File(*,filePath=" + filePath + ((int)(DateTime.Now-before).TotalMilliseconds) + " miliseconds after Starting");
            return true;
        }

        public void MyLog(String line)
        {
            DateTime now = DateTime.Now;
            Console.WriteLine("(id=" + id + ")("+now.Hour + ":"+now.Minute + ":"+now.Second+"." +now.Millisecond + ") " + line);
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
                logLine = "selectCommandRes="+selectCommandRes+ " After " + fromStart + " milliseconds";
                MyLog(logLine);
                

                String deleteCommand = "Delete";
                int deleteCommandRes = rhino.RunScript(deleteCommand, 1);
                fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                logLine = "deleteCommandRes="+selectCommandRes+ " After " + fromStart + " milliseconds";
                MyLog(logLine);
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
            MyLog(logLine);


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
