using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace UtilsDLL
{
    public class Image_Utils
    {
        public static bool shortCut(String filePath, out Color[] res)
        {
            res = null;
            try
            {
                Bitmap img = new Bitmap(filePath);
                res = new Color[img.Height];
                for (int i = 0; i < img.Height; i++)
                {
                    res[i] = img.GetPixel(i, i);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public static bool compare_shortcuts(Color[] s1, Color[] s2, out bool compRes)
        {
            compRes = false;
            if (s1.Length != s2.Length) return false;
            for (int i = 0; i < s1.Length; i++)
            {
                if (s1[i] != s2[i]) return true;
            }
            compRes = true;
            return true;
        }

    }
}
