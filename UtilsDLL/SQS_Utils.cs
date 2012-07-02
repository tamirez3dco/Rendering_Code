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
                if (!Get_Q_arn(Q_name, out Q_arn))
                {
                    Console.WriteLine("Get_Q_arn(Q_name="+Q_name+", out Q_arn) failed!!!");
                    return false;
                }
                return true;
            }
            return false;
        }


        public static bool Get_Q_arn(String Q_name, out String Q_arn)
        {
            Amazon.IdentityManagement.AmazonIdentityManagementServiceClient identity_client = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient();
            String accountId = identity_client.GetUser().GetUserResult.User.UserId;
            //String accountId = idMgr.getUser().getUser().getUserId();
            Q_arn = "arn:aws:sqs:us-east-1:"+accountId + ":" + Q_name;
            return true;
        }

        public static bool Delete_all_msgs_from_Q(String Q_name)
        {
            bool Q_found;
            String Q_url = String.Empty;
            String Q_arn = String.Empty;
            if (!Find_Q_By_name(Q_name,out Q_found,out Q_url,out Q_arn))
            {
                Console.WriteLine("UtilsDLL.SQS_Utils.Find_Q_By_name("+Q_name+",*,*) failed!!!");
                return false;
            }

            if (!Q_found)
            {
                Console.WriteLine("UtilsDLL.SQS_Utils.Find_Q_By_name("+Q_name+",*,*) succeeded but Q not found!!");
                return false;
            }


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

            foreach (String str in list_Qs_response.ListQueuesResult.QueueUrl)
            {
                if (str.EndsWith(Q_name))
                {
                    Q_found = true;
                    Q_url = str;
                    if (!Get_Q_arn(Q_name, out Q_arn))
                    {
                        Console.WriteLine("Get_Q_arn(Q_name="+Q_name+", out Q_arn) failed!!!");
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
