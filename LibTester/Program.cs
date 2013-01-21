using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino4;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Xml;


namespace LibTester
{
    class Program
    {
        static void Main(string[] args)
        {

            UtilsDLL.ThreeJS.convert_from_obj_to_js(@"C:\Temp\hope2.obj", @"C:\Temp\yalla2.js");
            System.Drawing.Text.InstalledFontCollection col = new System.Drawing.Text.InstalledFontCollection();
            FontFamily[] ff = col.Families;
            UtilsDLL.Dirs.get_all_relevant_dirs();
            Dictionary<String,bool> paramNames;
            Dictionary<String,Object> resDist = new Dictionary<string,object>();
            bool check1 = UtilsDLL.Rhino.Adjust_GHX_file(@"C:\Users\Administrator\Downloads\Fudged-Vorg-Test.ghx", @"C:\Users\Administrator\Downloads\Fudged-Vorg-Test_adj.ghx", resDist, new List<String>());
            return;
            bool paramsRes = UtilsDLL.Rhino.Get_All_Parameters_From_GHX_file(@"C:\Temp\t13.ghx", out paramNames);
            return;

            //String filePath = @"C:\Temp\iPhone_Lui16_base.ghx";
            String filePath = @"C:\Temp\test22.ghx";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            XmlNode root = xmlDoc.DocumentElement;


            //title[@lang='eng']	Selects all the title elements that have an attribute named lang with a value of 'eng'
            XmlNodeList objList = root.SelectNodes("//chunk[@name='Object']");

            
            foreach (XmlNode gh_obj in objList)
            {
                XmlNode node = gh_obj.SelectSingleNode("items/item[@name='Name']");
                if (node == null) continue;
                String nodeType = node.InnerText;

                bool isSlider = (nodeType == "Number Slider");
                if (isSlider)
                {
                    XmlNode attsNode = gh_obj.SelectSingleNode("chunks/chunk/chunks/chunk[@name='Slider']");


                    XmlNodeList attList = attsNode.SelectNodes("items/item");
                    Dictionary<String, Object> attsDict = new Dictionary<string, object>();
                    foreach (XmlNode attNode in attList)
                    {
                        String attName = attNode.Attributes["name"].Value;
                        String attValue = attNode.InnerText;
                        attsDict[attName] = attValue;
                        if (attName == "Description")
                        {
                            String newName = "ThingsMaker :  " + attValue;
                            attNode.InnerText = newName;
                        }
                        if (attName == "NickName")
                        {
                            String newName = "AUTO_" + attValue;
                            attNode.InnerText = newName;
                        }

                    }

                    String paramGUID = String.Empty, paramType = String.Empty;

                    int sliderType = int.Parse((String)(attsDict["Interval"]));

                    switch (sliderType)
                    {
                        case 0: // float slider
                            paramGUID = "3e8ca6be-fda8-4aaf-b5c0-3c54c8bb7312";
                            paramType = "Number";
                            break;
                        case 1: // integer slider
                        case 2: // odds slider
                        case 3: // evens slider
                            paramGUID = "2e3ab970-8545-46bb-836c-1c11e5610bce";
                            paramType = "Integer";
                            break;
                    }

                    XmlNode GUID_node = gh_obj.SelectSingleNode("items/item[@name='GUID']");
                    GUID_node.InnerText = paramGUID;

                    XmlNode Name_node = gh_obj.SelectSingleNode("items/item[@name='Name']");
                    Name_node.InnerText = paramType;

                    Name_node = gh_obj.SelectSingleNode("chunks/chunk/items/item[@name='Name']");
                    Name_node.InnerText = paramType;

                    XmlNode descriptionNode = gh_obj.SelectSingleNode("chunks/chunk/items/item[@name='Description']");
                    String newDescription = "Thingsmaker replacing (" + descriptionNode.InnerText + ")";
                    descriptionNode.InnerText = newDescription;

                    XmlNode nickNameNode = gh_obj.SelectSingleNode("chunks/chunk/items/item[@name='NickName']");
                    String newNickName = "AUTO_" + nickNameNode.InnerText;
                    nickNameNode.InnerText = newNickName;
                }
            }

            xmlDoc.Save(@"C:\Temp\test_99.ghx");


            return;
            
/*
            // kill all current Rhino4.exe processes
            Process[] procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Killing " + procs.Length + " previous Rhino processes");
            foreach (Process p in procs) { p.Kill(); }
            Thread.Sleep(1000);
            procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine(procs.Length + " previous Rhino processes remaind alive");



            Rhino5Application rhino_app = new Rhino5Application();
            rhino_app.Visible = 1;
            rhino_app.RunScript("_Grasshopper", 0);
            dynamic grasshopper = rhino_app.GetPlugInObject("b45a29b1-4343-4035-989e-044e8580d9cf", "00000000-0000-0000-0000-000000000000") as dynamic;
            grasshopper.OpenDocument(@"C:\inetpub\ftproot\Rendering_Data\GH_Def_files\test_str.gh");
            bool res = grasshopper.AssignDataToParameter("Str", "sababa");

            return;

*/
            UtilsDLL.Rhino.Rhino_Wrapper rhino_wrapper = null;

            if (!UtilsDLL.Rhino.start_a_SingleRhino("cases.3dm", true, out rhino_wrapper))
            {
                Console.WriteLine("Basa");
                return;
            }

            Grasshopper.Kernel.GH_DocumentIO io = new Grasshopper.Kernel.GH_DocumentIO();
            bool openRes = io.Open(@"C:\inetpub\ftproot\Rendering_Data\GH_Def_files\iPhone_txt_tst.gh");



            

            


            return;


            String[] allScenes = { "cases.3dm", "rings.3dm", "vases.3dm" };

            String basicPath = @"C:\inetpub\ftproot\empty_images_comparer";
            foreach (String scene_key in allScenes)
            {
                String scenePath = basicPath + Path.DirectorySeparatorChar + scene_key;
                if (!Directory.Exists(scenePath)) Directory.CreateDirectory(scenePath);
                if (!UtilsDLL.Rhino.start_a_SingleRhino(scene_key, true, out rhino_wrapper))
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

                String fullPath = viewPath + Path.DirectorySeparatorChar+ @"empty.jpg";
                bool res_4 = UtilsDLL.Rhino.Render(rhino_wrapper, new System.Drawing.Size(height, height), fullPath);


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
                    res_4 = UtilsDLL.Rhino.Render(rhino_wrapper, new System.Drawing.Size(height, height), fullPath);
                }
            }

            Dictionary<String, Object> dic = new Dictionary<string,object>();

            for (int i = 0; i < 4; i++)
            {
                dic["par1"] = 5 - i;
                dic["par2"] = 2 + i;

            bool res1 = UtilsDLL.Rhino.Set_GH_Params_To_TXT_File(rhino_wrapper, dic);

            bool res2 = UtilsDLL.Rhino.Open_GH_File(rhino_wrapper, UtilsDLL.Dirs.GH_DirPath + "/iPhone-frames-trial-release.gh");

            bool res3 = UtilsDLL.Rhino.Solve_GH(rhino_wrapper);

            bool res35 = UtilsDLL.Rhino.Bake_GH(rhino_wrapper, "Bakery");

            bool res4 = UtilsDLL.Rhino.Render(rhino_wrapper, new System.Drawing.Size(200, 200), @"C:\Temp\hope_" + i +".jpg");

            }
            //            grasshopper.RunSolver(true);

//            Object objRes = grasshopper.BakeDataInObject("G");

/*
            UtilsDLL.Dirs.get_all_relevant_dirs();

            UtilsDLL.Rhino.Rhino_Wrapper wrapper;

            // kill all current Rhino4.exe processes
            Process[] procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Killing " + procs.Length + " previous Rhino processes");
            foreach (Process p in procs) { p.Kill(); }
            Thread.Sleep(1000);
            procs = Process.GetProcessesByName("Rhino4");
            Console.WriteLine(procs.Length + " previous Rhino processes remaind alive");

            bool createRes = UtilsDLL.Rhino.start_a_SingleRhino("rings.3dm", true, out wrapper);

            bool loadRes = UtilsDLL.Rhino.Open_GH_File(wrapper, @"C:\inetpub\ftproot\Rendering_Data\GH_Def_files\test1.ghx");
 

            Dictionary<String,Object> dict = new Dictionary<string,object>();
            dict["4e459553-8255-4da2-915f-ebd9ee1c192b"] = 0.4;
            bool changeRes = UtilsDLL.Rhino.Set_GH_Params(wrapper,"M",dict);

            //UtilsDLL.Rhino.b
 */ 
        }
    }
}
