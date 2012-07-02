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
    
    public class SNS_Utils
    {
        public static AmazonSimpleNotificationServiceClient sns_client = new AmazonSimpleNotificationServiceClient();

        public static bool Find_Topic_By_Name(String topic_name,out bool topic_found, out String topic_arn)
        {
            List<Topic> all_topics;
            topic_found = false;
            topic_arn = String.Empty;
            if (!List_All_Topics(out all_topics))
            {
                Console.WriteLine("List_All_Topics () failed!!!");
                return false;
            }

            foreach (Topic t in all_topics)
            {
                if (t.TopicArn.EndsWith(topic_name))
                {
                    topic_found = true;
                    topic_arn = t.TopicArn;
                    break;
                }
            }
            return true;
        }
        public static bool List_All_Topics(out List<Topic> topics)
        {
            ListTopicsRequest lt_request = new ListTopicsRequest();
            ListTopicsResponse lt_response = sns_client.ListTopics(lt_request);
            topics = lt_response.ListTopicsResult.Topics;
            return true;

        }

        public static bool Add_SQS_Subscription(String topic_arn, String sqs_Q_listener_arn)
        {
            SubscribeRequest request = new SubscribeRequest();
            request.Protocol = "SQS";
            request.Endpoint = sqs_Q_listener_arn;
            request.TopicArn = topic_arn;
            SubscribeResponse response = sns_client.Subscribe(request);
            return true;
        }

        public static bool Find_Listener_By_Q_arn(String topic_name, String Q_arn, out bool listener_found)
        {
            listener_found = false;
            List<Subscription> all_listeners;
            if (!List_All_listeners(topic_name, out all_listeners))
            {
                Console.WriteLine("List_All_listeners(topic_name="+topic_name+", out all_listeners) failed!!!");
                return false;
            }

            foreach (Subscription s in all_listeners)
            {
                if (s.Protocol.ToLower() != "sqs") continue;
                if (s.Endpoint.ToLower() == Q_arn.ToLower())
                {
                    listener_found = true;
                    break;
                }
            }
            return true;
        }

        public static bool List_All_listeners(String topic_name, out List<Subscription> listeners)
        {
            bool topic_found;
            listeners = null;
            String topic_arn;
            if (!UtilsDLL.SNS_Utils.Find_Topic_By_Name(topic_name, out topic_found, out topic_arn))
            {
                Console.WriteLine("UtilsDLL.SNS_Utils.Find_Topic_By_Name() failed!!!");
                return false;
            }

            if (!topic_found)
            {
                Console.WriteLine("UtilsDLL.SNS_Utils.Find_Topic_By_Name() did not find topic_name="+topic_name +"!!!");
                return false;
            }

            ListSubscriptionsByTopicRequest request = new ListSubscriptionsByTopicRequest();
            request.TopicArn = topic_arn;
            ListSubscriptionsByTopicResponse response = sns_client.ListSubscriptionsByTopic(request);
            listeners = response.ListSubscriptionsByTopicResult.Subscriptions;
            return true;
        }
    //    public static 
    }
}
