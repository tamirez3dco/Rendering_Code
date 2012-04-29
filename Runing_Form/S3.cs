using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Runing_Form
{
    class S3
    {
        static AmazonS3 s3_client;
        public static String bucketName = null;


        public static bool Write_File_To_S3(String path, String key_name)
        {
            try
            {
                // simple object put
                PutObjectRequest request = new PutObjectRequest();
                request.WithFilePath(path)
                    .WithBucketName(bucketName)
                    .WithKey(key_name);

                S3Response response = s3_client.PutObject(request);
                response.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception n Write_File_To_S3(String path=" + path + ", String key_name=" + key_name + "). e.Message="+e.Message);
                Console.WriteLine("Write_File_To_S3(String path=" + path + ", String key_name=" + key_name + ") failed!!!");
                return false;
            }

            return true;

        }

        public static bool Initialize_S3_stuff()
        {
            Console.WriteLine("starting Initialize_S3_stuff()");

            s3_client = null;
            bucketName = null;
            try
            {
                if (!Utils.CFG.ContainsKey("s3_bucketName"))
                {
                    Console.WriteLine("param s3_bucketName is not found in ez3d.config");
                    return false;
                }
                s3_client = AWSClientFactory.CreateAmazonS3Client();

//                ListBucketsRequest listBucketsRequest = new ListBucketsRequest();
                ListBucketsResponse response = s3_client.ListBuckets();

                foreach (S3Bucket bucket in response.Buckets)
                {
                    if (bucket.BucketName == (String)Utils.CFG["s3_bucketName"])
                    {
                        bucketName = bucket.BucketName;
                        Console.WriteLine("bucketName =" + bucketName);
                    }
                }


                if (bucketName == null)
                {
                    Console.WriteLine("(bucketName == null)");
                    return false;
                }

                Console.WriteLine("Initialize_S3_stuff fininshed succefully");
                return true;
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("AmazonS3Exception caught !!!");
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
