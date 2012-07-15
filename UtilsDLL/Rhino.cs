using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino4;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Drawing;

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
            newRhino.grasshopper.HideEditor();

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

        public static bool Open_3dm_file(Rhino_Wrapper rhino_wrapper, String tdm_filePath)
        {
            save_3dm(rhino_wrapper, "C:\\Temp\\stam.3dm");
            rhino_wrapper.rhino_app.RunScript("-Open " + tdm_filePath, 1);
            return true;
        }

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
            String command = "-SaveAs " + filePath + " Enter Enter";
            rhino_wrapper.rhino_app.RunScript(command, 1);
            return true;
        }

        public static bool save_3dm(Rhino_Wrapper rhino_wrapper, string filePath)
        {
            String command = "-SaveAs " + filePath;
            rhino_wrapper.rhino_app.RunScript(command, 1);
            return true;
        }

        public static bool Set_GH_Params(Rhino_Wrapper rhino_wrapper, String bake, Dictionary<String, Object> parameters)
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
                    return false;
                }

                fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
                logLine = "After assigning param:" + paramName + " the value=" + value + " After " + fromStart + " milliseconds";
                log(logLine);

            }

            rhino_wrapper.grasshopper.RunSolver(true);

            Object objRes = rhino_wrapper.grasshopper.BakeDataInObject(bake);

            fromStart = (int)((DateTime.Now - beforeTime).TotalMilliseconds);
            logLine = "After baking object:" + bake + " After " + fromStart + " milliseconds";
            log(logLine);

            return true;

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
                    runCommand += " " + value + " Enter";
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
