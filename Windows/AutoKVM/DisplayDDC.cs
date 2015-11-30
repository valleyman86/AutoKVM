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

        public enum MonitorSource: int
        {
            dvi_1 = 3,
            vga = 1,
            hdmi = 17,
            composite = 5
        };

        public delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        public static void ToggleDisplaySources(MonitorSource source1, MonitorSource source2)
        {
            EnumMonitorsDelegate enumMonitorsDelegate = (IntPtr hMonitor, IntPtr hdcMonitor, ref DisplayDDC.Rect lprcMonitor, IntPtr dwData) => {
                return EnumMonitors(hMonitor, hdcMonitor, ref lprcMonitor, dwData, (int)source1, (int)source2);
            };
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, enumMonitorsDelegate, IntPtr.Zero);
        }

        private static bool EnumMonitors(IntPtr hMonitor, IntPtr hdcMonitor, ref DisplayDDC.Rect lprcMonitor, IntPtr dwData, int sourceCode1, int sourceCode2)
        {
            if (hMonitor.ToInt32() != 65537)
            {
                return true;
            }

            int physicalMonitorCount = 0;
            DisplayDDC.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitorCount);

            DisplayDDC.Physical_Monitor[] physicalMonitors;
            DisplayDDC.GetPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitors);

            for (int i = 0; i < physicalMonitors.Length; ++i)
            {
                IntPtr nullVal = IntPtr.Zero;
                Int32 currentValue;
                Int32 maxValue;
                DisplayDDC.GetVCPFeatureAndVCPFeatureReply(physicalMonitors[i].hPhysicalMonitor, 0x60, ref nullVal, out currentValue, out maxValue);

                int newSource = (currentValue == sourceCode1 ? sourceCode2 : sourceCode1);
                bool success = DisplayDDC.SetVCPFeature(physicalMonitors[i].hPhysicalMonitor, 0x60, newSource);
            }

            IntPtr physicalMonitorsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(physicalMonitors[0]) * physicalMonitors.Length);
            Marshal.StructureToPtr(physicalMonitors[0], physicalMonitorsPtr, false);
            DisplayDDC.DestroyPhysicalMonitors(physicalMonitors.Length, physicalMonitorsPtr);
            Marshal.FreeHGlobal(physicalMonitorsPtr);

            return true;
        }

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

        [DllImport("Dxva2.dll")]
        public static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, ref IntPtr makeNull, out Int32 currentValue, out Int32 maxValue);
    }
}
