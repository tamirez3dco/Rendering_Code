using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Runing_Form
{
    class Utils
    {
        public static Dictionary<String, String> CFG;
        public static bool Read_Cfg_File()
        {
            CFG = new Dictionary<string, string>();
            DirectoryInfo currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            String filePath = currentDir.Parent.Parent.Parent.FullName + Path.DirectorySeparatorChar + "Extras" + Path.DirectorySeparatorChar + "ez3d.cfg";
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not locate config file in path "+ filePath);
                return false;
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                String line;
                char[] tokenizer = { '=' };
                while ((line = sr.ReadLine()) != null)
                {
                    String[] tokens = line.Split(tokenizer, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length != 2)
                    {
                        Console.WriteLine("illigle line in cfg file is:" + line);
                        return false;
                    }
                    CFG[tokens[0].Trim()] = tokens[1].Trim();
                }
            }
            return true;
        }

        public static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes
                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes
                = System.Convert.FromBase64String(encodedData);
            string returnValue =
               System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
    }
}
