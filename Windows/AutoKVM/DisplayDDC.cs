using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace AutoKVM
{
    class DisplayDDC
    {
        #region Private members

        private const int PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Physical_Monitor
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PHYSICAL_MONITOR_DESCRIPTION_SIZE)]
            public string szPhysicalMonitorDescription;
        }

        // Operates on result of CapabilitiesRequestAndCapabilitiesReply(). Extracts vcp code 60 values into capture group 1.
        private static readonly string vcp60ValuesPattern = @"vcp\((?:.*?\(.*?\))*[^\(\)]*?60 ?\((.*?)\)";
        private static readonly Regex vcp60ValuesRegex = new Regex(vcp60ValuesPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static List<DisplayDDC.Physical_Monitor[]> physicalMonitors;

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        #endregion

        public static void Init()
        {
            physicalMonitors = new List<Physical_Monitor[]>();

            EnumMonitorsDelegate enumMonitorsDelegate = new EnumMonitorsDelegate(EnumMonitorsInit);
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, enumMonitorsDelegate, IntPtr.Zero);
        }

        public static void Cleanup()
        {
            foreach (Physical_Monitor[] monitors in physicalMonitors)
            {
                IntPtr monitorsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(monitors[0]) * monitors.Length);
                Marshal.StructureToPtr(monitors[0], monitorsPtr, false);
                DisplayDDC.DestroyPhysicalMonitors((uint)monitors.Length, monitorsPtr);
                Marshal.FreeHGlobal(monitorsPtr);
            }
        }

        public static List<int[]> GetMonitorSupportedSources()
        {
            List<int[]> sourcesList = new List<int[]>();

            foreach (Physical_Monitor[] monitorArray in physicalMonitors)
            {
                foreach (Physical_Monitor monitor in monitorArray)
                {
                    int[] sources = GetMonitorSupportedSources(monitor.hPhysicalMonitor);
                    sourcesList.Add(sources);
                }
            }

            return sourcesList;
        }
        public static void CycleDisplaySources(int monitorIndex, int[] enabledSources)
        {
            if (enabledSources.Length == 0)
            {
                return;
            }

            int currentMonitorIndex = 0;
            foreach (Physical_Monitor[] monitorArray in physicalMonitors)
            {
                foreach (Physical_Monitor monitor in monitorArray)
                {
                    if (currentMonitorIndex == monitorIndex)
                    {
                        IntPtr nullVal = IntPtr.Zero;
                        int currentValue;
                        int maxValue;
                        DisplayDDC.GetVCPFeatureAndVCPFeatureReply(monitor.hPhysicalMonitor, 0x60, ref nullVal, out currentValue, out maxValue);

                        int currentSourceIndex = Array.FindIndex(enabledSources, x => (x == currentValue)); // Can be -1, but we are ok with that.

                        int newSourceIndex = currentSourceIndex + 1;
                        if (newSourceIndex >= enabledSources.Length)
                        {
                            newSourceIndex = 0;
                        }

                        bool success = DisplayDDC.SetVCPFeature(monitor.hPhysicalMonitor, 0x60, enabledSources[newSourceIndex]);

                        return;
                    }

                    ++currentMonitorIndex;
                }
            }
        }

        #region Private functions

        private static bool EnumMonitorsInit(IntPtr hMonitor, IntPtr hdcMonitor, ref DisplayDDC.Rect lprcMonitor, IntPtr dwData)
        {
            uint physicalMonitorCount = 0;
            DisplayDDC.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitorCount);

            DisplayDDC.Physical_Monitor[] physicalMonitors;
            DisplayDDC.GetPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitors);

            DisplayDDC.physicalMonitors.Add(physicalMonitors);

            return true;
        }

        private static bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out Physical_Monitor[] pPhysicalMonitorArray)
        {
            uint physicalMonitorCount = 0;
            GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitorCount);

            Physical_Monitor[] monitors = new Physical_Monitor[physicalMonitorCount];
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(monitors[0]) * monitors.Length);

            bool success = GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount, p);

            IntPtr pointer = new IntPtr(p.ToInt64());
            pPhysicalMonitorArray = new Physical_Monitor[physicalMonitorCount];
            for (int i = 0; i < pPhysicalMonitorArray.Length; ++i)
            {
                pPhysicalMonitorArray[i] = (Physical_Monitor)Marshal.PtrToStructure(p, typeof(Physical_Monitor));
                pointer += Marshal.SizeOf(typeof(Physical_Monitor));
            }

            Marshal.FreeHGlobal(p);

            return success;
        }

        private static int[] GetMonitorSupportedSources(IntPtr hMonitor)
        {
            int[] values = new int[0];

            uint strSize;
            GetCapabilitiesStringLength(hMonitor, out strSize);

            StringBuilder capabilities = new StringBuilder((int)strSize);
            CapabilitiesRequestAndCapabilitiesReply(hMonitor, capabilities, strSize);

            Match match = vcp60ValuesRegex.Match(capabilities.ToString());
            if (match.Success)
            {
                string valuesStr = match.Groups[1].ToString().Trim();
                string[] valueArray = valuesStr.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                values = Array.ConvertAll(valueArray, s =>
                    int.Parse(s, System.Globalization.NumberStyles.HexNumber));
            }

            return values;
        }

        #endregion

        #region DLL imports

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("Dxva2.dll")]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("Dxva2.dll")]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, IntPtr pPhysicalMonitorArray);

        [DllImport("Dxva2.dll")]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, IntPtr pPhysicalMonitorArray);

        /// <summary>
        /// Used for features with continuous values (values that can be anything in [0, maxValue]).
        /// </summary>
        [DllImport("Dxva2.dll")]
        private static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, ref IntPtr makeNull, out int currentValue, out int maxValue);

        /// <summary>
        /// Retrieves the length of a monitor's capabilities string, including the terminating null character.
        /// </summary>
        [DllImport("Dxva2.dll")]
        private static extern bool GetCapabilitiesStringLength(IntPtr hMonitor, out uint numCharacters);
        
        /// <summary>
        /// Retrieves a string describing a monitor's capabilities.
        /// </summary>
        /// <param name="hMonitor">Handle to a physical monitor.</param>
        /// <param name="capabilities">The buffer must include space for the terminating null character. The result is in ASCII.</param>
        /// <param name="capabilitiesLength">Includes the terminating null character.</param>
        [DllImport("Dxva2.dll")]
        private static extern bool CapabilitiesRequestAndCapabilitiesReply(IntPtr hMonitor, StringBuilder capabilities, uint capabilitiesLength);

        [DllImport("Dxva2.dll")]
        private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, int dwNewValue);

        #endregion
    }
}
