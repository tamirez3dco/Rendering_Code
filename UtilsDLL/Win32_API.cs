using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;

namespace UtilsDLL
{
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }

    public class Win32_API
    {
        public const int WM_USER = 0x400;
        public const int WM_COPYDATA = 0x4A;

        //For use with WM_COPYDATA and COPYDATASTRUCT
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

        public static int sendWindowsStringMessage(int hWnd, int wParam, string msg)
        {
            int result = 0;
            byte[] sarr = System.Text.Encoding.Default.GetBytes(msg);
            int len = sarr.Length;
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)100;
            cds.lpData = msg;
            cds.cbData = len + 1;
            result = SendMessage(hWnd, WM_COPYDATA, wParam, ref cds);
            return result;
        }

        [DllImport("User32.dll")]
        public static extern Int32 FindWindow(String lpClassName, String lpWindowName);

        public static void Kill_Process(int pid)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "taskkill";
            psi.Arguments = "/F /pid " + pid;
            Process.Start(psi);
        }

        public static long GetFreeSpace()
       { 
            
        System.IO.DriveInfo C = new System.IO.DriveInfo("C");
        long cAvailableSpace = C.AvailableFreeSpace;
        return cAvailableSpace;


        }

    }


}
