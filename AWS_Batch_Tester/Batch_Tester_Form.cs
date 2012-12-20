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

using UtilsDLL;

namespace AWS_Batch_Tester
{
    public partial class Batch_Tester_Form : Form
    {

        public Batch_Tester_Form()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                String request_Q_url,request_Q_arn;
                bool sqs_Q_found;
                String Q_name = name_textBox.Text + "_";
                if (checkBox1.Checked) Q_name += "lowpriority_";
                Q_name += sceneTextBox.Text + "_request";




                if (!SQS_Utils.Find_Q_By_name(Q_name, out sqs_Q_found, out request_Q_url, out request_Q_arn))
                {
                    return;
                }
                if (!sqs_Q_found)
                {
                    return;
                }

                String[] layers = { "Default", "tamir1", "tamir2", "tamir3", "tamir4" };
                int first_id = int.Parse(id_textBox.Text);
                int num_msgs = int.Parse(id_till_textBox.Text);

                for (int j = 0; j < Math.Ceiling((double)num_msgs / 10); j++)
                {
                    List<String> msgEntries = new List<String>();
                    Dictionary<String, Object> dict = new Dictionary<String, Object>();


                    dict["gh_file"] = file_textBox.Text;
                    dict["view_name"] = viewTextBox.Text;
                    dict["scene"] = sceneTextBox.Text + ".3dm";
                    //dict["scene"] = "multiLayer.3dm";
                    //dict["scene"] = optionalScenes[(j/3)%optionalScenes.Length];
                    //if (i == 7) dict["scene"] = "no_such_file";
                    dict["item_id"] = (first_id).ToString();

                    dict["getSTL"] = getSTL_checkBox.Checked;
                    //dict["load_stl"] = "Gilad_RM";
                    dict["retries"] = 2;


                    //String layerName = (layers[i % layers.Length]);
                    //if (i <= 7) dict["layer_name"] = layerName;

                    String layerName = "Plastic_Matt_Red";
                    dict["layer_name"] = layerName;

                    dict["params"] = new Dictionary<String, Object>();
                    dict["width"] = 180;
                    dict["height"] = 180;
                    Dictionary<String, Object> paramsDict = new Dictionary<String, Object>();
                    //double propValue = (i % 2 == 0) ? 0.1 : 0.9;
                    Double val1 = double.Parse(value_textBox.Text);
                    paramsDict[property_textBox.Text] = (Object)(val1);
                    Double val2 = double.Parse(textBox1.Text);
                    paramsDict[textBox2.Text] = val2;

                    //paramsDict[property_textBox.Text] = propValue;
                    paramsDict["textParam"] = textValue_textBox.Text;
                    dict["params"] = paramsDict;
                    //List<String> bakeries = new List<String>();
                    //bakeries.Add(bake_textBox.Text);
                    dict["bake"] = bake_textBox.Text;
                    dict["operation"] = "render_model";

                    JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                    string jsonString = serializer.Serialize((object)dict);

                    msgEntries.Add(jsonString);
                    if (!SQS_Utils.Send_Msg_To_Q(request_Q_url, jsonString, true))
                    {
                        return;
                    }
                }
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
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox4.Text = SQS_Utils.DecodeFrom64(richTextBox3.Text);
        }

        void all()
        {
            try
            {
                String request_Q_url,request_Q_arn;
                bool sqs_Q_found;
                String Q_name = "tamir_cases_request";

                if (!SQS_Utils.Find_Q_By_name(Q_name, out sqs_Q_found, out request_Q_url, out request_Q_arn))
                {
                    return;
                }
                if (!sqs_Q_found)
                {
                    return;
                }


                int steps = 5;
                double step = 1.0 / (double)steps;

                Dictionary<String, Object> dict = new Dictionary<String, Object>();
                dict["params"] = new Dictionary<String, Object>();
                dict["width"] = 180;
                dict["height"] = 180;
                dict["getSTL"] = false;
                dict["gh_file"] = "iPhone_Lui16_base.gh";
                dict["view_name"] = "Render";
                dict["scene"] = "cases.3dm";
                dict["layer_name"] = "Plastic_Matt_Red";
                dict["retries"] = 3;
                Dictionary<String, Object> paramsDict = new Dictionary<String, Object>();
                
                dict["bake"] = bake_textBox.Text;
                dict["operation"] = "render_model";

                double[] a1_options = { 0, 0.6, 1 };
                foreach (double a1 in a1_options)
                {
                    paramsDict["a1"] = a1;

                    double[] a2_options = { 0, 0.2,0.4,0.6,0.8, 1 };
                    foreach (double a2 in a2_options)
                    {
                        paramsDict["a2"] = a2;
                        double[] a3_options = { 0, 0.2, 0.4, 0.6, 0.8, 1 };
                        foreach (double a3 in a3_options) 
                        {
                            paramsDict["a3"] = a3;

//                            for (int s4 = 0; s4 <= steps; s4++)
                            //{
                                paramsDict["a4"] = 0.6;

                                paramsDict["a5"] = 0.4;
                                paramsDict["a6"] = 0.4;

                                dict["item_id"] = String.Format("{0:0.##}", a1) + "_" + String.Format("{0:0.##}", a2) + "_" + String.Format("{0:0.##}", a3);
                                dict["params"] = paramsDict;

                                JavaScriptSerializer serializer = new JavaScriptSerializer(); //creating serializer instance of JavaScriptSerializer class
                                string jsonString = serializer.Serialize((object)dict);

                                if (!SQS_Utils.Send_Msg_To_Q(request_Q_url, jsonString, true))
                                {
                                    return;
                                }
//                            }

                        }
                    }
                }
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

        private void button4_Click(object sender, EventArgs e)
        {
            all();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
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

        private void file_textBox_TextChanged(object sender, EventArgs e)
        {
            String request_Q_url, request_Q_arn;
            bool sqs_Q_found;
            String Q_name = name_textBox.Text + "_" + sceneTextBox.Text + "_request";

            if (!SQS_Utils.Find_Q_By_name(Q_name, out sqs_Q_found, out request_Q_url, out request_Q_arn))
            {
                return;
            }
            if (!sqs_Q_found)
            {
                return;
            }


        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            String request_Q_url, request_Q_arn;
            bool sqs_Q_found;
            String Q_name = name_textBox.Text + "_";
            if (checkBox1.Checked) Q_name += "lowpriority_";
            Q_name += sceneTextBox.Text + "_request";

            if (!SQS_Utils.Find_Q_By_name(Q_name, out sqs_Q_found, out request_Q_url, out request_Q_arn))
            {
                return;
            }
            if (!sqs_Q_found)
            {
                return;
            }

            if (!SQS_Utils.Delete_all_msgs_from_Q(request_Q_url))
            {
                MessageBox.Show("Delete_all_msgs_from_Q(=" + request_Q_url + ") failed!!");
            }
            return;
        }
    }
}
