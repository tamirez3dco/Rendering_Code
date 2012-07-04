using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace UtilsDLL
{
    public class SQS_Utils
    {
        public static AmazonSQSClient sqs_client = new AmazonSQSClient();

        public static bool Get_Msg_From_Q(String q_url, out Message msg, out bool msg_found)
        {
            msg_found = false;
            msg = null;
            try
            {

                ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
                receiveMessageRequest.MaxNumberOfMessages = 1;
                receiveMessageRequest.QueueUrl = q_url;
                ReceiveMessageResponse receiveMessageResponse = sqs_client.ReceiveMessage(receiveMessageRequest);
                if (receiveMessageResponse.IsSetReceiveMessageResult())
                {
                    ReceiveMessageResult receiveMessageResult = receiveMessageResponse.ReceiveMessageResult;
                    List<Amazon.SQS.Model.Message> receivedMsges = receiveMessageResponse.ReceiveMessageResult.Message;
                    if (receivedMsges.Count == 0)
                    {
                        return true;
                    }
                    msg_found = true;
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



        public static bool Create_Q(String Q_name, out String Q_url, out String Q_arn)
        {
            Q_url = String.Empty;
            Q_arn = String.Empty;
            CreateQueueRequest cq_request = new CreateQueueRequest();
            cq_request.QueueName = Q_name;
            CreateQueueResponse cq_response = sqs_client.CreateQueue(cq_request);
            Q_url = cq_response.CreateQueueResult.QueueUrl;
            if (Q_url.EndsWith(Q_name))
            {
                if (!Get_Q_arn(Q_url, out Q_arn))
                {
                    Console.WriteLine("Get_Q_arn(Q_name=" + Q_name + ", out Q_arn) failed!!!");
                    return false;
                }
                return true;
            }
            return false;
        }

        public static bool stam(String Q_url)
        {
            GetQueueAttributesRequest gqa_request = new GetQueueAttributesRequest();
            gqa_request.AttributeName = new List<String>(new String[] { "All" });
            gqa_request.QueueUrl = Q_url;
            GetQueueAttributesResponse gqa_response = sqs_client.GetQueueAttributes(gqa_request);
            return true;
        }

        public static bool Get_All_Q_Attributes(String Q_url, out List<Amazon.SQS.Model.Attribute> attributes)
        {
            attributes = null;
            GetQueueAttributesRequest gqa_request = new GetQueueAttributesRequest();
            gqa_request.AttributeName = new List<String>(new String[] { "All" });
            gqa_request.QueueUrl = Q_url;
            GetQueueAttributesResponse gqa_response = sqs_client.GetQueueAttributes(gqa_request);
            attributes = gqa_response.GetQueueAttributesResult.Attribute;
            return true;
        }


        public static bool Add_Q_Premissions_Everybody(String Q_url)
        {
            String sid = "\"Sid\":\"" + Q_url + "\"";
            String effect = "\"Effect\":\"Allow\"";
            String pricipal = "\"Principal\":{\"AWS\":\"*\"}";
            String action = "\"Action\":\"SQS:*\"";
            String Q_arn;
            if (!Get_Q_arn(Q_url, out Q_arn)) return false;
            String resource = "\"Resource\":\"" + Q_arn + "\"";

            String myPolicy = "{\"Version\":\"2008-10-17\",\"Id\":\"" + Q_arn + "/SQSDefaultPolicy\",\"Statement\":[{" +
                sid + "," + effect + "," + pricipal + "," + action + "," + resource + "}]}";
            SetQueueAttributesRequest sqa_request = new SetQueueAttributesRequest();
            sqa_request.Attribute = new List<Amazon.SQS.Model.Attribute>();
            Amazon.SQS.Model.Attribute att = new Amazon.SQS.Model.Attribute();
            att.Name = "Policy";
            att.Value = myPolicy;

            sqa_request.Attribute.Add(att);
            sqa_request.QueueUrl = Q_url;
            SetQueueAttributesResponse sqa_response = sqs_client.SetQueueAttributes(sqa_request);
            return true;
        }

        public static bool Get_Q_Attribute(String Q_url, String att_name, out String att_value)
        {
            att_value = String.Empty;
            List<Amazon.SQS.Model.Attribute> attributes;
            if (!Get_All_Q_Attributes(Q_url, out attributes)) return false;
            foreach (Amazon.SQS.Model.Attribute att in attributes)
            {
                if (att.Name == att_name)
                {
                    att_value = att.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool Get_Q_arn(String Q_url, out String Q_arn)
        {
            return Get_Q_Attribute(Q_url, "QueueArn", out Q_arn);
        }

        public static bool Delete_all_msgs_from_Q(String Q_url)
        {
            while (true)
            {
                Message msg;
                bool msg_found = false;
                if (!Get_Msg_From_Q(Q_url, out msg, out msg_found))
                {
                    Console.WriteLine("UtilsDLL.SQS_Utils.Get_Msg_From_Q(Q_url=" + Q_url + ",*,*) succeeded but no message found!!");
                    return false;
                }
                if (!msg_found) break;

                if (!Delete_Msg_From_Q(Q_url, msg))
                {
                    Console.WriteLine("UtilsDLL.SQS_Utils.Delete_Msg_From_Q(Q_url=" + Q_url + ",*,*) succeeded but no message found!!");
                    return false;
                }
            }

            return true;
        }



        public static bool Find_Q_By_name(String Q_name, out bool Q_found, out String Q_url, out String Q_arn)
        {
            ListQueuesRequest list_Qs_request = new ListQueuesRequest();
            ListQueuesResponse list_Qs_response = sqs_client.ListQueues(list_Qs_request);

            Q_found = false;
            Q_url = String.Empty;
            Q_arn = String.Empty;

            foreach (String q_url in list_Qs_response.ListQueuesResult.QueueUrl)
            {
                if (q_url.EndsWith(Q_name))
                {
                    Q_found = true;
                    Q_url = q_url;
                    if (!Get_Q_arn(q_url, out Q_arn))
                    {
                        Console.WriteLine("Get_Q_arn(Q_name=" + Q_name + ", out Q_arn) failed!!!");
                        return false;
                    }
                    break;
                }
            }

            return true;
        }

        public static bool Delete_Msg_From_Q(String Q_url, Message msg)
        {
            try
            {
                String messageRecieptHandle = msg.ReceiptHandle;
                Console.WriteLine("Deleting the message.\n");
                DeleteMessageRequest deleteRequest = new DeleteMessageRequest();
                deleteRequest.QueueUrl = Q_url;
                deleteRequest.ReceiptHandle = messageRecieptHandle;
                sqs_client.DeleteMessage(deleteRequest);

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
