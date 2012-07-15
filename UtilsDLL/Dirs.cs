using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace UtilsDLL
{
    public class Dirs
    {
        private static bool get_Scenes_Dir(out String scenesDirPath)
        {
            scenesDirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            scenesDirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Rendering_Data" + Path.DirectorySeparatorChar + "Scene";
            if (!Directory.Exists(scenesDirPath))
            {
                Console.WriteLine("ERROR!!! - Could not find ScenesDir=" + scenesDirPath);
                return false;

            }
            Console.WriteLine("ScenesDir = " + scenesDirPath);
            return true;
        }

        private static bool get_grasshopper_files_Dir(out String GH_DirPath)
        {
            GH_DirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            GH_DirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Rendering_Data" + Path.DirectorySeparatorChar + "GH_Def_files";
            if (!Directory.Exists(GH_DirPath))
            {
                Console.WriteLine("ERROR!!! - Could not find GH_DirPathr=" + GH_DirPath);
                return false;

            }
            Console.WriteLine("GH_DirPath = " + GH_DirPath);
            return true;
        }

        private static bool get_pythonscripts_files_Dir(out String PY_DirPath)
        {
            PY_DirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            PY_DirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Rendering_Data" + Path.DirectorySeparatorChar + "PythonScripts";
            if (!Directory.Exists(PY_DirPath))
            {
                Console.WriteLine("ERROR!!! - Could not find PY_DirPath=" + PY_DirPath);
                return false;

            }
            Console.WriteLine("PY_DirPath = " + PY_DirPath);
            return true;
        }

        public static bool get_tempImages_files_Dir(out String image_DirPath)
        {
            image_DirPath = null;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            image_DirPath = dir.Parent.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "tempImageFiles";
            if (!Directory.Exists(image_DirPath))
            {
                bool creationSuccess = false;
                try
                {
                    DirectoryInfo createDif = Directory.CreateDirectory(image_DirPath);
                    creationSuccess = createDif.Exists;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR !!! - Exception in get_tempImages_files_Dir()");
                    Console.WriteLine(e.Message);
                    creationSuccess = false;
                }
                if (!creationSuccess)
                {
                    Console.WriteLine("ERROR!!! - Could not find or create image_DirPath=" + image_DirPath);
                    return false;
                }
            }
            Console.WriteLine("image_DirPath = " + image_DirPath);
            return true;
        }

        public static String scenes_DirPath;
        public static String GH_DirPath;
        public static String images_DirPath;
        public static String PythonScripts_DirPath_Git;
        public static String PythonScripts_DirPath_Actual;

        public static bool get_all_relevant_dirs()
        {
            if (!get_Scenes_Dir(out scenes_DirPath)) return false;
            if (!get_grasshopper_files_Dir(out GH_DirPath)) return false;
            if (!get_tempImages_files_Dir(out images_DirPath)) return false;
            if (!get_pythonscripts_files_Dir(out PythonScripts_DirPath_Git)) return false;

            PythonScripts_DirPath_Actual = @"C:\Users\" + System.Environment.UserName + @"\AppData\Roaming\McNeel\Rhinoceros\5.0\Plug-ins\PythonPlugins\quest {4aa421bc-1d5d-4d9e-9e48-91bf91516ffa}\dev";
            return true;
        }

        public static bool Refresh_Rhino_GH_Data_From_Github()
        {
            // Pull data (all under ftproot/Rendering_Data from GitHub)
            DirectoryInfo currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());

            String renderingDataFolderPath = currentDir.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Extras";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("stash.bat");
                psi.WorkingDirectory = renderingDataFolderPath;

                Process p = Process.Start(psi);
                //p.WaitForInputIdle();
                p.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Refresh_Rhino_GH_Data_From_Github(1). e.Message=" + e.Message);
            }


            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("pullData.bat");
                psi.WorkingDirectory = renderingDataFolderPath;

                Process p = Process.Start(psi);
                p.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Refresh_Rhino_GH_Data_From_Github(2). e.Message=" + e.Message);
            }



            // copy PythonScripts to correct location
            DirectoryInfo pythonScriptsGitDirectory = new DirectoryInfo(PythonScripts_DirPath_Git);
            FileInfo[] pythonFiles = pythonScriptsGitDirectory.GetFiles("*.py");
            foreach (FileInfo pythonFile in pythonFiles)
            {
                String destFileName = PythonScripts_DirPath_Actual + Path.DirectorySeparatorChar + pythonFile.Name;
                pythonFile.CopyTo(destFileName, true);
            }
            return true;
        }

    }
}
