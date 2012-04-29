using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon;
using System.Web.Script.Serialization;

// Add using statements to access AWS SDK for .NET services. 
// Both the Service and its Model namespace need to be added 
// in order to gain access to a service. For example, to access
// the EC2 service, add:
// using Amazon.EC2;
// using Amazon.EC2.Model;

namespace AWS_Batch_Tester
{
    class Program
    {
        public static void Main(string[] args)
        {
            Runing_Form.Utils.user_Data_String = args[0];
            Console.WriteLine("Runing_Form.Utils.user_Data_String="+Runing_Form.Utils.user_Data_String);
            new Batch_Tester_Form().ShowDialog();
            Console.Read();
        }
    }
}