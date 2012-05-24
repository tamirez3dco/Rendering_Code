using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Web.Script.Serialization;

namespace Runing_Form
{
    class SQS
    {
        public static AmazonSQS sqs;
        public static String requests_Q_url = null;
        public static String ready_Q_url = null;
        public static String error_Q_url = null;
        public static String error_Q_name = null;



        public static bool Initialize_SQS_stuff()
        {
            Console.WriteLine("starting Initialize_SQS_stuff()");

            sqs = null;
            ready_Q_url = requests_Q_url = null;
            error_Q_url = null;
            error_Q_name = "GENERAL_ERROR";
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
                if (Utils.CFG.ContainsKey("error_Q_name"))
                {
                    Console.WriteLine("param error_Q_name found in ez3d.config=" + Utils.CFG["error_Q_name"]);
                    error_Q_name = (String)Utils.CFG["error_Q_name"];
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
                        if (str.EndsWith('/' + (String)Utils.CFG["request_Q_name"]))
                        {
                            requests_Q_url = str;
                            Console.WriteLine("requests_Q_url =" + requests_Q_url);
                        }
                        if (str.EndsWith('/' + (String)Utils.CFG["ready_Q_name"]))
                        {
                            ready_Q_url = str;
                            Console.WriteLine("ready_Q_url =" + ready_Q_url);
                        }
                        if (str.EndsWith('/' + error_Q_name))
                        {
                            error_Q_url = str;
                            Console.WriteLine("error_Q_url =" + error_Q_url);
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
                    if (error_Q_url == null)
                    {
                        Console.WriteLine("(error_Q_url == null)");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("listQueuesResponse.IsSetListQueuesResult() == false");
                    return false;
                }

                Console.WriteLine("Initialize_SQS_stuff fininshed succefully");
                return true;
            }
            catch (Amazon.SQS.AmazonSQSException e)
            {
                Console.WriteLine("Amazon SQS caught !!!");
                Console.WriteLine(e.Message);
                return false;
            }
        }


        public static bool Send_Server_Ready_Message()
        {
            try
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();

                dict["server_ip"] = Utils.my_ip;
                dict["public_dns"] = Utils.public_dns;
                dict["num_of_rhinos_on_server"] = Runing_Form.num_of_rhinos;
                dict["request_Q_name"] = Utils.CFG["request_Q_name"];
                dict["ready_Q_name"] = Utils.CFG["ready_Q_name"];

                JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                string jsonString = serializer.Serialize((object)dict);

                SendMessageRequest sendMessageRequest = new SendMessageRequest();
                sendMessageRequest.QueueUrl = SQS.ready_Q_url; //URL from initial queue creation
                sendMessageRequest.MessageBody = Utils.EncodeTo64(jsonString);

                Console.WriteLine("Before sending ready msg(" + sendMessageRequest.MessageBody + ").");
                sqs.SendMessage(sendMessageRequest);
                Console.WriteLine("After sending ready msg(" + sendMessageRequest.MessageBody + ").");
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

        public static bool Get_Msg_From_Req_Q(out Amazon.SQS.Model.Message msg, out bool msgFound)
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

        public static bool Send_Msg_To_Readies_Q(RenderStatus status, String item_id, DateTime beforeProcessingTime)
        {
            try
            {

                Dictionary<string, object> dict = new Dictionary<string, object>();

                DateTime current = DateTime.Now;
                TimeSpan duration = current - beforeProcessingTime;

                dict["item_id"] = item_id;
                //dict["url"] = @"http://" + Utils.my_ip + @"/testim/yofi_" + item_id + ".jpg";
                dict["url"] = @"http://s3.amazonaws.com/" + S3.bucketName + @"/" + item_id + ".jpg";
                dict["duration"] = Math.Round(duration.TotalSeconds, 3);
                dict["status"] = status.ToString();

                JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                string jsonString = serializer.Serialize((object)dict);

                SendMessageRequest sendMessageRequest = new SendMessageRequest();
                sendMessageRequest.QueueUrl = ready_Q_url; //URL from initial queue creation
                sendMessageRequest.MessageBody = Utils.EncodeTo64(jsonString);

                Console.WriteLine("Before sending ready msg(" + sendMessageRequest.MessageBody + ").");
                sqs.SendMessage(sendMessageRequest);
                Console.WriteLine("After sending ready msg(" + sendMessageRequest.MessageBody + ").");
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

        public static bool Delete_Msg_From_Req_Q(Amazon.SQS.Model.Message msg)
        {
            try
            {
                String messageRecieptHandle = msg.ReceiptHandle;

                //Deleting a message
                Console.WriteLine("Deleting the message.\n");
                DeleteMessageRequest deleteRequest = new DeleteMessageRequest();
                deleteRequest.QueueUrl = requests_Q_url;
                deleteRequest.ReceiptHandle = messageRecieptHandle;
                Console.WriteLine("Before deleting incoming msg(" + messageRecieptHandle + ").");
                sqs.DeleteMessage(deleteRequest);
                Console.WriteLine("After deleting incoming msgs().");

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


        internal static bool Send_Msg_To_ERROR_Q(int id, string msg)
        {
            try
            {

                Dictionary<string, object> dict = new Dictionary<string, object>();

                DateTime current = DateTime.Now;

                dict["GHR_id"] = id;
                dict["msg"] = msg;
                dict["ip"] = Utils.my_ip;
                dict["time"] = current.Hour.ToString() + ":" + current.Minute + ":" + current.Second;

                JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                string jsonString = serializer.Serialize((object)dict);

                SendMessageRequest sendMessageRequest = new SendMessageRequest();
                sendMessageRequest.QueueUrl = error_Q_url; //URL from initial queue creation
                sendMessageRequest.MessageBody = jsonString;

                Console.WriteLine("Before sending ready msg(" + sendMessageRequest.MessageBody + ").");
                sqs.SendMessage(sendMessageRequest);
                Console.WriteLine("After sending ready msg(" + sendMessageRequest.MessageBody + ").");
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
    }
}
