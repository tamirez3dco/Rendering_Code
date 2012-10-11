using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using System.Web.Script.Serialization;
using System.IO;

namespace UtilsDLL
{
    public class S3_Utils
    {
        static AmazonS3Client s3_client = new AmazonS3Client();

        public static bool Make_Sure_Bucket_Exists(String bucket_name)
        {
            bool bucket_found = false;
            String bucket_arn;
            if (!Find_Bucket(bucket_name, out bucket_found, out bucket_arn))
            {
                Console.WriteLine("Find_Bucket(bucket_name=" + bucket_name + ", out bucket_found) failed!!!");
                return false;
            }
            if (!bucket_found)
            {
                if (!Create_Bucket(bucket_name))
                {
                    Console.WriteLine("Create_Bucket(bucket_name=" + bucket_name + ", out bucket_found) failed!!!");
                    return false;
                }

                Thread.Sleep(60000);
            }
            return true;
        }

        public static bool Create_Bucket(string bucket_name)
        {
            PutBucketRequest create_request = new PutBucketRequest();
            create_request.BucketName = bucket_name;

            PutBucketResponse response = s3_client.PutBucket(create_request);
            return true;
        }

        public static bool Add_Global_Read_premissions(String bucket_name, String bucket_arn)
        {

            PutBucketPolicyRequest request = new PutBucketPolicyRequest();
            request.BucketName = bucket_name;
            Dictionary<String,Object> dict = new Dictionary<string,object>();
            dict["Version"] = "2008-10-17";
            Dictionary<String,Object> inner_1 = new Dictionary<string,object>();
            inner_1["Sid"] = bucket_name + "_all_read";
            inner_1["Effect"] = "Allow";
            inner_1["Principal"] = "{\"AWS\": \"*\"}";
            inner_1["Action"] = "[\"s3:GetObject\"]";
            inner_1["Resource"] = "[\""+bucket_arn+"\"]";
            dict["Statement"] = inner_1;

            JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
            string jsonString = serializer.Serialize((object)dict);

            request.Policy = jsonString;

            PutBucketPolicyResponse response = s3_client.PutBucketPolicy(request);

            return true;
        }

        public static bool Find_Bucket(string bucket_name, out bool bucket_found, out String bucket_arn)
        {
            bool f;
            String Q_arn;
            String Q_url;
            UtilsDLL.SQS_Utils.Find_Q_By_name("deploy_request", out f, out Q_url, out Q_arn);

            bucket_found = false;
            bucket_arn = String.Empty;
            ListBucketsResponse response = s3_client.ListBuckets();

            foreach (S3Bucket bucket in response.Buckets)
            {
                if (bucket.BucketName == bucket_name)
                {
                    bucket_found = true;
                    //bucket_arn =  s3_client.get
                    break;
                }
            }

            return true;
        }

        public static bool Write_File_To_S3(String bucket_name, String path, String key_name)
        {
            DateTime before = DateTime.Now;
            try
            {
                // simple object put
                PutObjectRequest request = new PutObjectRequest();
                request.WithFilePath(path)
                    .WithBucketName(bucket_name)
                    .WithKey(key_name);

                S3Response response = s3_client.PutObject(request);
                response.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception n Write_File_To_S3(String path=" + path + ", String key_name=" + key_name + "). e.Message=" + e.Message);
                Console.WriteLine("Write_File_To_S3(String path=" + path + ", String key_name=" + key_name + ") failed!!!");
                return false;
            }
            TimeSpan uploadTime = DateTime.Now - before;
            Console.WriteLine("Uploading " + path + " into S3 Bucket=" + bucket_name + " , key=" + key_name + " took " + uploadTime.TotalMilliseconds.ToString() + " milliseconds");

            return true;

        }

        public static bool Download_File_From_S3(String bucketName, String destination_localPath, String keyName)
        {
            DateTime before = DateTime.Now;

            try
            {
                GetObjectRequest request = new GetObjectRequest().WithBucketName(bucketName).WithKey(keyName);

                using (GetObjectResponse response = s3_client.GetObject(request))
                {
                    string title = response.Metadata["x-amz-meta-title"];
                    if (!File.Exists(destination_localPath))
                    {
                        response.WriteResponseStreamToFile(destination_localPath);
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Caught Exception: " + e.Message);

                Type exType = e.GetType();
                if (exType == typeof(AmazonS3Exception))
                {
                    AmazonS3Exception amazonS3Exception = (AmazonS3Exception)e;
                    if (amazonS3Exception.ErrorCode != null &&
                        (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                        amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                    {
                        Console.WriteLine("Please check the provided AWS Credentials.");
                        Console.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                    }
                    else
                    {
                        Console.WriteLine("An error occurred with the message '{0}' when reading an object", amazonS3Exception.Message);
                    }
                }
                return false;
            }

            TimeSpan downloadTime = DateTime.Now - before;
            Console.WriteLine("Downloading from s3  Bucket=" + bucketName + " into path=" + destination_localPath + " took " + downloadTime.TotalMilliseconds.ToString() + " milliseconds");

            return true;

        }
    
    
    
    }
}
