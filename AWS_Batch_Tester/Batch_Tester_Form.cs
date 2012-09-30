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
                String Q_name = name_textBox.Text + "_" + sceneTextBox.Text + "_request";

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
                double initialValue = double.Parse(value_textBox.Text);
                double delta = double.Parse(delta_textBox.Text);
                //[{"gh_file":"brace1.gh","item_id":501,"NumCircles":0.5,"bake":"Bracelet"}]
                String[] optionalScenes = { "scene11.3dm", "scene13.3dm", "scene15.3dm", "scene17.3dm" };
                for (int j = 0; j < Math.Ceiling((double)num_msgs / 10); j++)
                {
                    List<String> msgEntries = new List<String>();
                    for (int i = j * 10; i < Math.Min(num_msgs, (j + 1) * 10); i++)
                    {
                        Dictionary<String, Object> dict = new Dictionary<String, Object>();


                        dict["gh_file"] = file_textBox.Text;
                        dict["view_name"] = viewTextBox.Text;
                        dict["scene"] = sceneTextBox.Text +".3dm";
                        //dict["scene"] = "multiLayer.3dm";
                        //dict["scene"] = optionalScenes[(j/3)%optionalScenes.Length];
                        //if (i == 7) dict["scene"] = "no_such_file";
                        dict["item_id"] = (first_id + i).ToString();

                        dict["getSTL"] = getSTL_checkBox.Checked;


                        //String layerName = (layers[i % layers.Length]);
                        //if (i <= 7) dict["layer_name"] = layerName;

                        String layerName = "Clay";
                        dict["layer_name"] = layerName;

                        dict["params"] = new Dictionary<String, Object>();
                        dict["width"] = 180;
                        dict["height"] = 180;
                        Dictionary<String, Object> paramsDict = new Dictionary<String, Object>();
                        double propValue = Math.Round(initialValue + ((i) % 10) * delta, 1);
                        //double propValue = (i % 2 == 0) ? 0.1 : 0.9;
                        paramsDict["a1"] = 0.4;
                        paramsDict["a2"] = 0.1;
                        paramsDict["a3"] = 0.1;
                        paramsDict["a4"] = 0.5;
                        paramsDict["a5"] = 0.5;
                        paramsDict["a6"] = 0.5;
                        paramsDict[property_textBox.Text] = 0.16 * i;
                        //paramsDict["textParam"] = "Ahlan Wasahalan";
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
            richTextBox4.Text = Runing_Form.Utils.DecodeFrom64(richTextBox3.Text);
        }
    }
}
