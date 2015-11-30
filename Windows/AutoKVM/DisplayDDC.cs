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
        #region Members

        internal struct MonitorSource
        {
            public int code;
            public string name;
        }

        #region Private members

        private const int PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;

        private enum VCPCodes : byte
        {
            InputSource = 0x60
        }

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
        // Operates on result of CapabilitiesRequestAndCapabilitiesReply(). Extracts the MCCS version.
        private static readonly string mccsVersionPattern = @"mccs_ver\((.*?)\)";
        private static readonly Regex vcp60ValuesRegex = new Regex(vcp60ValuesPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex mccsVersionRegex = new Regex(mccsVersionPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Sources in MCCS v2.0 == v2.1, and both are a subset of 2.2, so we use a single array to cover them all.
        // Note that the standards use one-based indexing, so we just add a dummy element at the start.
        private static readonly string[] sourceNamesMccsV2 = {
            "**undefined**",
            "VGA 1",
            "VGA 2",
            "DVI 1",
            "DVI 2",
            "Composite 1",
            "Composite 2",
            "S-video 1",
            "S-video 2",
            "Tuner 1",
            "Tuner 2",
            "Tuner 3",
            "Component 1",
            "Component 2",
            "Component 3",
            "DisplayPort 1",
            "DisplayPort 2",
            "HDMI 1",
            "HDMI 2"
        };

        // Note that MCCS v3.0 was not well adopted, so 2.2a has become the active standard.
        // Note that the standards use one-based indexing, so we just add a dummy element at the start.
        private static readonly string[] sourceNamesMccsV3 = {
            "**undefined**",
            "VGA 1",
            "VGA 2",
            "DVI 1",
            "DVI 2",
            "Composite 1",
            "Composite 2",
            "S-video 1",
            "S-video 2",
            "Tuner - Analog 1",
            "Tuner - Analog 2",
            "Tuner - Digital 1",
            "Tuner - Digital 2",
            "Component 1",
            "Component 2",
            "Component 3",
            "**Unrecognized**",
            "DisplayPort 1",
            "DisplayPort 2"
        };

        private static List<DisplayDDC.Physical_Monitor[]> physicalMonitors;

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        #endregion
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

        /// <summary>
        /// Returns a list of MonitorSources, one per monitor.
        /// </summary>
        public static List<MonitorSource[]> GetMonitorSupportedSources()
        {
            List<MonitorSource[]> sourcesList = new List<MonitorSource[]>();

            foreach (Physical_Monitor[] monitorArray in physicalMonitors)
            {
                foreach (Physical_Monitor monitor in monitorArray)
                {
                    MonitorSource[] sources = GetMonitorSupportedSources(monitor.hPhysicalMonitor);
                    sourcesList.Add(sources);
                }
            }

            return sourcesList;
        }

        /// <summary>
        /// Returns a list of monitor input source codes, one per monitor.
        /// Values correspond to MonitorSource.code.
        /// </summary>
        public static List<int> GetMonitorActiveSources()
        {
            List<int> activeSources = new List<int>();

            foreach (Physical_Monitor[] monitorArray in physicalMonitors)
            {
                foreach (Physical_Monitor monitor in monitorArray)
                {
                    IntPtr nullVal = IntPtr.Zero;
                    int currentValue;
                    int maxValue;
                    GetVCPFeatureAndVCPFeatureReply(monitor.hPhysicalMonitor, (byte)VCPCodes.InputSource, ref nullVal, out currentValue, out maxValue);

                    activeSources.Add(currentValue);
                }
            }

            return activeSources;
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
                        GetVCPFeatureAndVCPFeatureReply(monitor.hPhysicalMonitor, (byte)VCPCodes.InputSource, ref nullVal, out currentValue, out maxValue);

                        int currentSourceIndex = Array.FindIndex(enabledSources, x => (x == currentValue)); // Can be -1, but we are ok with that.

                        int newSourceIndex = currentSourceIndex + 1;
                        if (newSourceIndex >= enabledSources.Length)
                        {
                            newSourceIndex = 0;
                        }

                        bool success = SetVCPFeature(monitor.hPhysicalMonitor, (byte)VCPCodes.InputSource, enabledSources[newSourceIndex]);

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
                pPhysicalMonitorArray[i] = (Physical_Monitor)Marshal.PtrToStructure(pointer, typeof(Physical_Monitor));
                pointer += Marshal.SizeOf(typeof(Physical_Monitor));
            }

            Marshal.FreeHGlobal(p);

            return success;
        }

        private static MonitorSource[] GetMonitorSupportedSources(IntPtr hMonitor)
        {
            int[] values = new int[0];

            uint strSize;
            GetCapabilitiesStringLength(hMonitor, out strSize);

            StringBuilder capabilities = new StringBuilder((int)strSize);
            CapabilitiesRequestAndCapabilitiesReply(hMonitor, capabilities, strSize);
            string capabilitiesStr = capabilities.ToString();

            // Parse source codes.
            Match match = vcp60ValuesRegex.Match(capabilitiesStr);
            if (match.Success)
            {
                string valuesStr = match.Groups[1].Value.Trim();
                string[] valueArray = valuesStr.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                values = Array.ConvertAll(valueArray, s =>
                    int.Parse(s, System.Globalization.NumberStyles.HexNumber));
            }

            // Parse MCCS version.
            string[] sourceNames = new string[0];
            match = mccsVersionRegex.Match(capabilitiesStr);
            if (match.Success)
            {
                string versionStr = match.Groups[1].Value.Trim();
                string[] versionArray = versionStr.Split(new char[]{'.'}, StringSplitOptions.RemoveEmptyEntries);
                int majorVersion = int.Parse(versionArray[0]);

                if (majorVersion < 3)
                    sourceNames = sourceNamesMccsV2;
                else
                    sourceNames = sourceNamesMccsV3;
            }

            // Prepare output.
            MonitorSource[] sources = new MonitorSource[values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                sources[i].code = values[i];
                if (0 <= values[i] && values[i] < sourceNames.Length)
                    sources[i].name = sourceNames[values[i]];
                else
                    sources[i].name = "**Unrecognized**";
            }

            return sources;
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
