using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Web.Script.Serialization;

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace AWS_Batch_Tester
{
    public partial class Batch_Tester_Form : Form
    {
        AmazonSQS client;
        String requests_Q_url;

        public Batch_Tester_Form()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {

                int first_id = int.Parse(id_textBox.Text);
                int num_msgs = int.Parse(id_till_textBox.Text);
                double initialValue = double.Parse(value_textBox.Text);
                double delta = double.Parse(delta_textBox.Text);
                //[{"gh_file":"brace1.gh","item_id":501,"NumCircles":0.5,"bake":"Bracelet"}]
                List<SendMessageBatchRequestEntry> msgEntries = new List<SendMessageBatchRequestEntry>();
                for (int i = 0; i < num_msgs; i++)
                {
                    Dictionary<String, Object> dict = new Dictionary<String, Object>();


                    dict["gh_file"] = file_textBox.Text;
                    dict["item_id"] = (first_id + i);
                    dict["params"] = new Dictionary<String,Object>();
                    Dictionary<String, Object> paramsDict = new Dictionary<String, Object>();
                    double propValue = Math.Round(initialValue + i * delta,1);
                    paramsDict[property_textBox.Text]=propValue;
                    dict["params"] = paramsDict;
                    //List<String> bakeries = new List<String>();
                    //bakeries.Add(bake_textBox.Text);
                    dict["bake"] = bake_textBox.Text;
                    dict["operation"] = "render_model";

                    JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                    string jsonString = serializer.Serialize((object)dict);

                    SendMessageBatchRequestEntry entry = new SendMessageBatchRequestEntry();
                    entry.MessageBody =  Runing_Form.Utils.EncodeTo64(jsonString);
                    entry.Id = i.ToString();
                    msgEntries.Add(entry);
                }

                SendMessageBatchRequest sendMessageBatchRequest = new SendMessageBatchRequest();
                sendMessageBatchRequest.QueueUrl = requests_Q_url;
                sendMessageBatchRequest.Entries = msgEntries;
                client.SendMessageBatch(sendMessageBatchRequest);
            }
            catch (AmazonSQSException ex)
            {
                MessageBox.Show("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
                Console.WriteLine("XML: " + ex.XML);
                return;
            }
            return;

        }

        private void Batch_Tester_Form_Load(object sender, EventArgs e)
        {
            Runing_Form.Utils.Read_Cfg_File();


            client = AWSClientFactory.CreateAmazonSQSClient();

            requests_Q_url = null;
            ListQueuesRequest listQueuesRequest = new ListQueuesRequest();
            ListQueuesResponse listQueuesResponse = client.ListQueues(listQueuesRequest);
            if (listQueuesResponse.IsSetListQueuesResult())
            {
                ListQueuesResult listQueuesResult = listQueuesResponse.ListQueuesResult;
                foreach (String str in listQueuesResult.QueueUrl)
                {
                    Console.WriteLine("  QueueUrl: {0}", str);
                    if (str.EndsWith('/'+ (String)Runing_Form.Utils.CFG["request_Q_name"]))
                    {
                        requests_Q_url = str;
                    }
                }

                if (requests_Q_url == null)
                {
                    MessageBox.Show("(requests_Q_url == null)");
                    return;
                }
            }
            else
            {
                MessageBox.Show("listQueuesResponse.IsSetListQueuesResult() == false");
                return;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox4.Text = Runing_Form.Utils.DecodeFrom64(richTextBox3.Text);
        }
    }
}
