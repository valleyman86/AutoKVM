// Adapted from "Low-Level Keyboard Hook in C#" by Stephen Toub (MSFT), 3 May 2006
// http://blogs.msdn.com/b/toub/archive/2006/05/03/589423.aspx
//
// 31 May 2015: adapted by Brandon Morton.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoKVM
{
    class InterceptKeys
    {
        #region Members

        public delegate void KeyDownCallback(Keys key);

        #region Private members

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc hookCallback = HookCallback;
        private static KeyDownCallback keyDownCallback;
        private static IntPtr hookID = IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
        #endregion

        public static void Init()
        {
            hookID = SetHook(hookCallback);
        }

        public static void Cleanup()
        {
            UnhookWindowsHookEx(hookID);
        }

        public static void RegisterCallback(KeyDownCallback keyDownCallback)
        {
            InterceptKeys.keyDownCallback = keyDownCallback;
        }

        #region Private functions

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                if (keyDownCallback != null)
                {
                    keyDownCallback(key);
                }
            }

            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        #endregion

        #region DLL imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}
