using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace UtilsDLL
{
    public class ThreeJS
    {
        public static bool convert_from_obj_to_js(String inputPath, String outputPath)
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine("convert_from_obj_to_js() failed because inputPath=" + inputPath + " not found!!!");
                return false;
            }
            if (File.Exists(outputPath))
            {
                Console.WriteLine("Warning at convert_from_obj_to_js() because outputPath=" + outputPath + " exists!");
                File.Delete(outputPath);
                System.Threading.Thread.Sleep(150);
                if (File.Exists(outputPath))
                {
                    Console.WriteLine("convert_from_obj_to_js() failed because could not delete existing oututPath=" + outputPath);
                    return false;
                }
            }

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = "python.exe";
            psi.WorkingDirectory = @"C:\Python27";
            psi.Arguments = @"c:\inetpub\ftproot\Rendering_Code\ThreeJS\mrdoob-three.js-0e6f58c\utils\converters\obj\convert_obj_three.py " +
                             "-t binary -i " + inputPath + " -o " + outputPath;
            psi.UseShellExecute = true;
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
            p.WaitForExit(10000);
            System.Threading.Thread.Sleep(300);
            if (!File.Exists(outputPath))
            {
                Console.WriteLine("Conversion failed in convert_from_obj_to_js(inputPath="+inputPath+", outputPath="+outputPath+")");
                return false;
            }

            return true;
        }
    }
}
