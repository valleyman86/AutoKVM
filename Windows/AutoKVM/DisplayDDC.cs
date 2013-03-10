using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AutoKVM
{
    class DisplayDDC
    {
        public const int PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Physical_Monitor
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PHYSICAL_MONITOR_DESCRIPTION_SIZE)]
            public string szPhysicalMonitorDescription;
        }

        public delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("Dxva2.dll")]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out int pdwNumberOfPhysicalMonitors);

        [DllImport("Dxva2.dll")]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, int dwPhysicalMonitorArraySize, IntPtr pPhysicalMonitorArray);

        public static bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out Physical_Monitor[] pPhysicalMonitorArray)
        {
            int physicalMonitorCount = 0;
            GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitorCount);

            Physical_Monitor[] monitors = new Physical_Monitor[physicalMonitorCount];
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(monitors[0]) * monitors.Length);

            bool success = GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount, p);

            IntPtr pointer = new IntPtr(p.ToInt64());
            pPhysicalMonitorArray = new Physical_Monitor[physicalMonitorCount];
            for(int i = 0; i < pPhysicalMonitorArray.Length; ++i) {
                pPhysicalMonitorArray[i] = (Physical_Monitor)Marshal.PtrToStructure(p, typeof(Physical_Monitor));
                pointer += Marshal.SizeOf(typeof(Physical_Monitor));
            }

            Marshal.FreeHGlobal(p);

            return success;
        }

        [DllImport("Dxva2.dll")]
        public static extern bool DestroyPhysicalMonitors(int dwPhysicalMonitorArraySize, IntPtr pPhysicalMonitorArray);

        [DllImport("Dxva2.dll")]
        public static extern bool  SetVCPFeature(IntPtr hMonitor, byte bVCPCode,  int dwNewValue);
    }
}
