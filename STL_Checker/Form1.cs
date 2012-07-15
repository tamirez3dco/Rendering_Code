using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using UtilsDLL;

namespace STL_Checker
{
    public partial class Form1 : Form
    {
        private static Rhino.Rhino_Wrapper rhino_wrapper;
        //private static RhinoScript4.RhinoScript scriper;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool dirsRes = UtilsDLL.Dirs.get_all_relevant_dirs();
            bool rhinoRes = UtilsDLL.Rhino.start_a_SingleRhino("rings.3dm", true, out rhino_wrapper);

            while (true)
            {
                Dictionary<String, Object> parameters = new Dictionary<string, object>();
                Random r = new Random();
                for (int i = 0; i < 9; i++)
                {
                    parameters["a"+i.ToString()] = 0.1 * r.Next(0,9);
                }
                single_cycle("brace2", parameters);
            }
        }


        private static bool single_cycle(String script_name, Dictionary<String,Object> parameters)
        {
            Rhino.DeleteAll(rhino_wrapper);
            Rhino.Run_Script(rhino_wrapper, script_name, parameters);
            //bool unify_res = Rhino.Unify_1(rhino_wrapper);
            bool unify_res = true;
            if (unify_res)
            {
                String jpg_filePath = @"C:\Temp\" + script_name + "_";
                foreach (String key in parameters.Keys)
                {
                    jpg_filePath += key + "_" + parameters[key] + "_";
                }
                jpg_filePath += ".jpg";
                Rhino.Render(rhino_wrapper,  "Render", new Size(200, 200), jpg_filePath);

                String stl_filePath = jpg_filePath.Replace(".jpg", ".stl");
                Rhino.save_stl(rhino_wrapper, stl_filePath);

            }
            return true;
        }


    }
}
