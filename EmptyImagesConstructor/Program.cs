using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace EmptyImagesConstructor
{
    class Program
    {
        static void Main(string[] args)
        {
            // kill all current Rhino4.exe processes
            Process[] procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Killing " + procs.Length + " previous Rhino processes");
            foreach (Process p in procs) { p.Kill(); }
            Thread.Sleep(1000);
            procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine(procs.Length + " previous Rhino processes remaind alive");
            UtilsDLL.Dirs.get_all_relevant_dirs();

            /*

                        Rhino5Application rhino_app = new Rhino5Application();
                        rhino_app.Visible = 1;
                        rhino_app.RunScript("_Grasshopper", 0);
                        dynamic grasshopper = rhino_app.GetPlugInObject("b45a29b1-4343-4035-989e-044e8580d9cf", "00000000-0000-0000-0000-000000000000") as dynamic;
                        grasshopper.OpenDocument(@"C:\inetpub\ftproot\Rendering_Data\GH_Def_files\iPhone-frames-trial-release.gh");
                        //bool res = grasshopper.AssignDataToParameter("Num", (Object)0.5);
            */
            JavaScriptSerializer ser = new JavaScriptSerializer();
            Object[] allScenes = (Object[])ser.DeserializeObject(args[0]);


            String basicPath = @"C:\inetpub\ftproot\empty_images_comparer";
            UtilsDLL.Rhino.Rhino_Wrapper rhino_wrapper = null;
            foreach (String scene_obj in allScenes)
            {
                String scenePath = basicPath + Path.DirectorySeparatorChar + (String)scene_obj;
                if (!Directory.Exists(scenePath)) Directory.CreateDirectory(scenePath);
                if (!UtilsDLL.Rhino.start_a_SingleRhino((String)scene_obj + ".3dm", true, out rhino_wrapper))
                {
                    Console.WriteLine("Basa");
                    return;
                }

                int height = 180;
                String size_key = height + "_" + height;
                String sizePath = scenePath + Path.DirectorySeparatorChar + size_key;
                if (!Directory.Exists(sizePath)) Directory.CreateDirectory(sizePath);

                String onlyView = "Render";
                String viewPath = sizePath + Path.DirectorySeparatorChar + onlyView;
                if (!Directory.Exists(viewPath)) Directory.CreateDirectory(viewPath);

                String fullPath = viewPath + Path.DirectorySeparatorChar + @"empty.jpg";
                bool res_4 = UtilsDLL.Rhino.Render(rhino_wrapper, onlyView, new System.Drawing.Size(height, height), fullPath);


                height = 350;
                size_key = height + "_" + height;
                sizePath = scenePath + Path.DirectorySeparatorChar + size_key;
                if (!Directory.Exists(sizePath)) Directory.CreateDirectory(sizePath);
                String[] allViews = { "Render", "Top", "Front" };
                foreach (String view_key in allViews)
                {

                    viewPath = sizePath + Path.DirectorySeparatorChar + view_key;
                    if (!Directory.Exists(viewPath)) Directory.CreateDirectory(viewPath);
                    fullPath = viewPath + Path.DirectorySeparatorChar + @"empty.jpg";
                    res_4 = UtilsDLL.Rhino.Render(rhino_wrapper, view_key, new System.Drawing.Size(height, height), fullPath);
                }
            }

        }
    }
}
