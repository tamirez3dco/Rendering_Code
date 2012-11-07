using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino4;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Xml;
using System.Windows.Forms;

namespace UtilsDLL
{
    public class Rhino
    {
        public class Rhino_Wrapper
        {
            public Rhino5Application rhino_app;
            public int rhino_pid;
            public DateTime creationTime;
            public DateTime killTime;
            public dynamic grasshopper;
        }


        public enum Cycle_Result
        {
            NO_MSG,
            ERROR,
            SUCCESS
        }

        private static void log(String str)
        {
            Console.WriteLine(str);
        }

        public static bool DeleteAll(Rhino_Wrapper rhino_wrapper)
        {
            DateTime beforeTime = DateTime.Now;
            String logLine;
            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "Starting DeleteAll()";
            log(logLine);


            // Delete all
            String deleteAllCommand = "EZ3DDellAllCommand";
            int deleteAllCommanddRes = rhino_wrapper.rhino_app.RunScript(deleteAllCommand, 1);
            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "deleteAllCommanddRes=" + deleteAllCommanddRes + " After " + fromStart + " milliseconds";
            log(logLine);
            return true;

        }

        public static bool setDefaultLayer(Rhino_Wrapper rhino_wrapper, string layerName)
        {
            String setLayerCommand = "_EZ3DSilentChangeLayerCommand " + layerName;
            int setLayerCommandRes = rhino_wrapper.rhino_app.RunScript(setLayerCommand, 1);
            return true;
        }

        public static bool Load_STL(Rhino_Wrapper rhino_wrapper, string filePath)
        {
            log("Starting  Load_STL(*,filePath=" + filePath);
            DateTime before = DateTime.Now;

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("ERROR!!: filePath=" + filePath + " does not exists");
                    return false;
                }

                String importCommand = "-Import " + filePath + " Enter";
                int importCommandRes = rhino_wrapper.rhino_app.RunScript(importCommand, 1);
                rhino_wrapper.rhino_app.RunScript("ChangeToCurrentLayer", 1);
            }
            catch (Exception e)
            {
                log("Exception=" + e.Message);
                return false;
            }

            log("Finished succefully  Load_STL(*,filePath=" + filePath + ((int)(DateTime.Now - before).TotalMilliseconds) + " miliseconds after Starting");
            return true;

        }

        public static bool Get_All_Parameters_From_GHX_file(String local_raw_ghx_path, out Dictionary<String,bool> paramNames)
        {
            paramNames = new Dictionary<string, bool>();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(local_raw_ghx_path);
                XmlNode root = xmlDoc.DocumentElement;

                XmlNodeList objList = root.SelectNodes("//chunk[@name='Object']");

                foreach (XmlNode gh_obj in objList)
                {
                    XmlNode node = gh_obj.SelectSingleNode("items/item[@name='Name']");
                    if (node == null) continue;
                    String nodeType = node.InnerText;

                    bool isNumberParam = (nodeType == "Number");
                    bool isIntegerParam = (nodeType == "Integer");
                    if (isNumberParam || isIntegerParam)
                    {
                        XmlNode nickNameNode = gh_obj.SelectSingleNode("chunks/chunk/items/item[@name='NickName']");
                        String nickName = nickNameNode.InnerText;
                        paramNames[nickName] = true;
                    }
                }
            }
            catch (Exception e)
            {
                log("Exception in Get_All_Parameters_From_GHX_file. e.Message=" + e.Message);
                return false;
            }


            return true;

        }


        public static bool Adjust_GHX_file(String local_raw_ghx_path, String local_adjusted_ghx_path, Dictionary<String,Object> reply, List<String> screener)
        {
            
            try
            {
                Dictionary<String,bool> currentParamNames;
                if (!Get_All_Parameters_From_GHX_file(local_raw_ghx_path, out currentParamNames))
                {
                    log("Get_All_Parameters_From_GHX_file(local_raw_ghx_path=" + local_raw_ghx_path + ")  failed!!!");
                    return false;
                }


                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(local_raw_ghx_path);
                XmlNode root = xmlDoc.DocumentElement;

                XmlNodeList objList = root.SelectNodes("//chunk[@name='Object']");

                List<Object> slidersList = new List<object>();
                foreach (XmlNode gh_obj in objList)
                {
                     XmlNode nickNameNode = gh_obj.SelectSingleNode("chunks/chunk/items/item[@name='NickName']");
                     String oldNickName = nickNameNode.InnerText;

                    if (screener != null && screener.Count > 0)
                    {
                        if (!screener.Contains(oldNickName)) continue;
                    }
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

                        Double minValue = double.Parse((String)(attsDict["Min"]));
                        Double maxValue = double.Parse((String)(attsDict["Max"]));
                        Double currentValue = double.Parse((String)(attsDict["Value"]));
                        Dictionary<String, Object> slider_dict = new Dictionary<string, object>();
                        slider_dict["min"] = minValue;
                        slider_dict["max"] = maxValue;
                        slider_dict["current"] = currentValue;

                        XmlNode GUID_node = gh_obj.SelectSingleNode("items/item[@name='GUID']");
                        GUID_node.InnerText = paramGUID;

                        XmlNode Name_node = gh_obj.SelectSingleNode("items/item[@name='Name']");
                        Name_node.InnerText = paramType;

                        Name_node = gh_obj.SelectSingleNode("chunks/chunk/items/item[@name='Name']");
                        Name_node.InnerText = paramType;

                        XmlNode descriptionNode = gh_obj.SelectSingleNode("chunks/chunk/items/item[@name='Description']");
                        String newDescription = "Thingsmaker replacing (" + descriptionNode.InnerText + ")";
                        descriptionNode.InnerText = newDescription;

                        String newNickName = "AUTO_" + oldNickName;
                        if (currentParamNames.ContainsKey(newNickName))
                        {
                            int unique_counter = 1;
                            while (true)
                            {
                                newNickName = "AUTO_" + oldNickName+"_" + unique_counter.ToString();
                                if (!currentParamNames.ContainsKey(newNickName)) break;
                                unique_counter++;
                            }
                        }
                        currentParamNames[newNickName] = true;
                        nickNameNode.InnerText = newNickName;
                        slider_dict["new_name"] = newNickName;
                        slider_dict["old_name"] = oldNickName;


                        slidersList.Add(slider_dict);
                    }
                }

                reply["sliders"] = slidersList;
                xmlDoc.Save(local_adjusted_ghx_path);
            }
            catch (Exception e)
            {
                log("Exception in Adjust_GHX_file. e.Message=" + e.Message);
                return false;
            }


            return true;

        }

        
        public static bool Save_GH_File(Rhino_Wrapper rhino_wrapper, string filePath)
        {
            log("Starting  Save_GH_File(*,filePath=" + filePath);
            DateTime before = DateTime.Now;

            try
            {
                rhino_wrapper.grasshopper.SaveDocumentAs(filePath);
            }
            catch (Exception e)
            {
                log("Exception=" + e.Message);
                return false;
            }

            log("Finished succefully  Save_GH_File(*,filePath=" + filePath + ((int)(DateTime.Now - before).TotalMilliseconds) + " miliseconds after Starting");
            return true;

        }


        public static bool Open_GH_File(Rhino_Wrapper rhino_wrapper, string filePath)
        {
            log("Starting  Open_GH_File(*,filePath=" + filePath);
            DateTime before = DateTime.Now;

            try
            {
                rhino_wrapper.grasshopper.CloseAllDocuments();
                Thread.Sleep(1000);
                rhino_wrapper.grasshopper.OpenDocument(filePath);
            }
            catch (Exception e)
            {
                log("Exception=" + e.Message);
                return false;
            }

            log("Finished succefully  Open_GH_File(*,filePath=" + filePath + ((int)(DateTime.Now - before).TotalMilliseconds) + " miliseconds after Starting");
            return true;

        }





        public static bool start_a_SingleRhino(String sceneFile_name, bool rhino_visible, out UtilsDLL.Rhino.Rhino_Wrapper newRhino)
        {
            newRhino = new UtilsDLL.Rhino.Rhino_Wrapper();

            Process[] procs_before = Process.GetProcessesByName("Rhino4");
            Console.WriteLine("Starting Rhino at " + DateTime.Now);
            newRhino.rhino_app = new Rhino5Application();
            newRhino.rhino_app.ReleaseWithoutClosing = 1;
            newRhino.rhino_app.Visible = rhino_visible ? 1 : 0;
            if (newRhino == null)
            {
                Console.WriteLine("rhino == null");
                return false;
            }
            for (int tries = 0; tries < 200; tries++)
            {
                if (newRhino.rhino_app.IsInitialized() == 1)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            Process[] procs_after = Process.GetProcessesByName("Rhino4");
            List<int> pids_before = new List<int>();
            List<int> new_pids = new List<int>();
            foreach (Process p in procs_before) pids_before.Add(p.Id);
            foreach (Process p in procs_after)
            {
                if (!pids_before.Contains(p.Id))
                {
                    new_pids.Add(p.Id);
                }
            }

            if (new_pids.Count != 1)
            {
                Console.WriteLine("ERROR ! - (new_pids.Count != 1 =" + new_pids.Count);
                Console.WriteLine("procs_before=:");
                foreach (Process p in procs_before) Console.WriteLine(p.Id);
                Console.WriteLine("procs_after=:");
                foreach (Process p in procs_after) Console.WriteLine(p.Id);
                return false;
            }
            else
            {
                newRhino.rhino_pid = new_pids[0];
            }

            
            Console.WriteLine("Starting Grasshopper at " + DateTime.Now);
            newRhino.rhino_app.RunScript("_Grasshopper", 0);
            Thread.Sleep(1000);
            newRhino.grasshopper = newRhino.rhino_app.GetPlugInObject("b45a29b1-4343-4035-989e-044e8580d9cf", "00000000-0000-0000-0000-000000000000") as dynamic;
            if (newRhino.grasshopper == null)
            {
                Console.WriteLine("ERROR!!: (grasshopper == null)");
                return false;
            }
            
            if (!rhino_visible) newRhino.grasshopper.HideEditor();

            String sceneFilePath = UtilsDLL.Dirs.scenes_DirPath + Path.DirectorySeparatorChar + sceneFile_name;
            if (!File.Exists(sceneFilePath))
            {
                Console.WriteLine("ERROR!!: sceneFilePath=" + sceneFilePath + " does not exists");
                return false;
            }

            String openCommand = "-Open " + sceneFilePath;
            int openCommandRes = newRhino.rhino_app.RunScript(openCommand, 1);

            int isInitialized = newRhino.rhino_app.IsInitialized();
            if (isInitialized != 1)
            {
                return false;
            }

            // load scene
            return true;
        }


        public static bool Render(Rhino_Wrapper rhino_wrapper, String viewName, Size size, string resultingImagePath)
        {
            try
            {
                if (!SetView(rhino_wrapper, viewName))
                {
                    log("Rhino.SetView(viewName=" + viewName + ") failed!!!");
                    return false;
                }

                DateTime beforeTime = DateTime.Now;
                String captureCommand = "-FlamingoRenderTo f " + resultingImagePath + " " + size.Width + " " + size.Height;
                int captureCommandRes = rhino_wrapper.rhino_app.RunScript(captureCommand, 1);

                int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                log("After rendering by: " + captureCommand + "  into" + resultingImagePath + " After " + fromStart + " milliseconds");
            }
            catch (Exception e)
            {
                log("Excpetion in Render(imageData=" + resultingImagePath + ", String outputPath=" + resultingImagePath + ", e.Message=" + e.Message);
                return false;
            }

            return true;

        }


        public static bool scaleAll(Rhino_Wrapper rhino_wrapper, double scaleRatio)
        {
            RhinoScript4.RhinoScript scripter = rhino_wrapper.rhino_app.GetScriptObject();
            scripter.AllObjects(true, false, false);
            //rhino_wrapper.rhino_app.RunScript("SelAll",1);
            rhino_wrapper.rhino_app.RunScript("-Scale 0,0,0 " + scaleRatio.ToString(), 1);
            return true;
        }

        public static bool SetView(Rhino_Wrapper rhino_wrapper, String viewName)
        {
            int intRes = rhino_wrapper.rhino_app.RunScript("-SetActiveViewport " + viewName, 1);
            return (intRes == 1);
        }
/*
        public static bool Open_3dm_file(Rhino_Wrapper rhino_wrapper, String tdm_filePath)
        {
            save_3dm(rhino_wrapper, "C:\\Temp\\stam.3dm");
            rhino_wrapper.rhino_app.RunScript("-Open " + tdm_filePath, 1);
            return true;
        }
*/
        public static bool Unify_1(Rhino_Wrapper rhino_wrapper)
        {
            int res;
            res = rhino_wrapper.rhino_app.RunScript("SelPolysrf", 1);
            res = rhino_wrapper.rhino_app.RunScript("BooleanUnion", 1);
            if (res != 1) return false;
            if (count_objects(rhino_wrapper) == 1) return true;
            res = rhino_wrapper.rhino_app.RunScript("SelSrf", 1);
            res = rhino_wrapper.rhino_app.RunScript("BooleanUnion", 1);
            if (res != 1) return false;
            if (count_objects(rhino_wrapper) == 1) return true;
            res = rhino_wrapper.rhino_app.RunScript("SelPolysrf", 1);
            res = rhino_wrapper.rhino_app.RunScript("BooleanUnion", 1);
            if (count_objects(rhino_wrapper) == 1) return true;
            return false;
        }

        public static void stam(Rhino_Wrapper rhino_wrapper)
        {
            RhinoScript4.RhinoScript scripter = rhino_wrapper.rhino_app.GetScriptObject();
            dynamic dyn = scripter.AllObjects();
            String[] objStrings = (String[])dyn;
            //scripter.GetObject(
        }

        public static int count_objects(Rhino_Wrapper rhino_wrapper)
        {
            RhinoScript4.RhinoScript scripter = rhino_wrapper.rhino_app.GetScriptObject();
            Object[] objs = scripter.AllObjects();
            return objs.Length;
        }

        public static bool save_stl(Rhino_Wrapper rhino_wrapper, string filePath)
        {
            //String command = "-SaveAs " + filePath + " Enter Enter";
            String command = "-SelLayer Default";
            rhino_wrapper.rhino_app.RunScript(command, 1);
            command = "-Export _GeometryOnly=Yes " + filePath;
            rhino_wrapper.rhino_app.RunScript(command, 1);
            return true;
        }

        public static bool save_3dm(Rhino_Wrapper rhino_wrapper, string filePath)
        {
            String command = "-SaveAs " + filePath;
            rhino_wrapper.rhino_app.RunScript(command, 1);
            return true;
        }
/*
        public static bool Solve_And_Bake(Rhino_Wrapper rhino_wrapper, String bake)
        {

            Object obj1 = rhino_wrapper.grasshopper.RunSolver(true);

            Object objRes = rhino_wrapper.grasshopper.BakeDataInObject(bake);

            return true;
        }
*/
        public static bool Solve_GH(Rhino_Wrapper rhino_wrapper)
        {

            Object obj1 = rhino_wrapper.grasshopper.RunSolver(true);
            return true;
        }

        public static bool Bake_GH(Rhino_Wrapper rhino_wrapper, String bake)
        {

            Object objRes = rhino_wrapper.grasshopper.BakeDataInObject(bake);
            return true;
        }

        public static bool Set_GH_Params(Rhino_Wrapper rhino_wrapper, Dictionary<String, Object> parameters)
        {
            log("Starting Set_GH_Params()");
            DateTime beforeTime = DateTime.Now;
            String logLine;
            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);


            foreach (String paramName in parameters.Keys)
            {

                Object value = parameters[paramName];
                if (!rhino_wrapper.grasshopper.AssignDataToParameter(paramName, value))
                {
                    fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                    logLine = "grasshopper.AssignDataToParameter(paramName=" + paramName + ", value=" + value + ") returned false After " + fromStart + " milliseconds";
                    log(logLine);
                    //return false;
                }

                fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                logLine = "After assigning param:" + paramName + " the value=" + value + " After " + fromStart + " milliseconds";
                log(logLine);

            }

            return true;

        }


        public static bool Set_GH_Params_To_TXT_File(Rhino_Wrapper rhino_wrapper, Dictionary<String, Object> parameters)
        {
            for (int tries = 0; tries < 5; tries++)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(UtilsDLL.Dirs.GH_DirPath + Path.DirectorySeparatorChar + "args_" + rhino_wrapper.rhino_pid + ".txt"))
                    {
                        String[] keys = new String[parameters.Count];
                        parameters.Keys.CopyTo(keys, 0);
                        Array.Sort(keys);
                        for (int i = 0 ; i < keys.Length ; i++)
                        {
                            String key = keys[i];
                            Object val = parameters[key];
                            sw.WriteLine(val);
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    log("Exception in Set_GH_Params_To_TXT_File(). e.Message=" + e.Message);
                }
            }

            return false;

        }


        public static bool Run_Script(Rhino_Wrapper rhino_wrapper, String scriptName, Dictionary<String, Object> parameters)
        {
            DateTime beforeTime = DateTime.Now;
            try
            {
                String commParams = "";
                List<String> stringValues = new List<string>();
                foreach (String paramName in parameters.Keys)
                {
                    Object propValue = parameters[paramName];
                    Type propValueType = propValue.GetType();
                    if (propValueType == typeof(Double) || propValueType == typeof(Decimal))
                    {
                        commParams = commParams + " " + paramName + "=" + parameters[paramName].ToString();
                    }
                    else if (propValue.GetType() == typeof(String))
                    {
                        stringValues.Add((String)propValue);
                    }
                }

                //String runCommand = "vase1 rad1=0.2 rad2=0.42 rad3=0.6 rad4=0.5 Enter";
                String runCommand = scriptName + " " + commParams + " Enter";
                foreach (String value in stringValues)
                {
                    runCommand += " \"" + value + "\" Enter";
                }
                rhino_wrapper.rhino_app.RunScript(runCommand, 1);
            }
            catch (Exception e)
            {
                log("Exception in Run_Script_And_Render(). e.Message=" + e.Message);
                return false;
            }


            int fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            log("Script ran After " + fromStart + " milliseconds");

            return true;

        }

    }
}
