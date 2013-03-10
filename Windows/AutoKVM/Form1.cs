using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace AutoKVM
{
    public partial class Form1 : Form
    {
        

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.ShowInTaskbar = false; 
            this.Visible = false;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button.Equals(MouseButtons.Left)) {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon1, null);
            }
        }

        private void switchAllToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void switchDisplaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisplayDDC.EnumMonitorsDelegate enumMonitorsDelegate = new DisplayDDC.EnumMonitorsDelegate(EnumMonitors);
            DisplayDDC.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, enumMonitorsDelegate, IntPtr.Zero);
        }

        private void switchKeyboardMouseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShareCentralIO usbSwitch = new ShareCentralIO();

            ShareCentralIO.Devices status = usbSwitch.GetStatusOfDevices();
            bool device1Status = (status & ShareCentralIO.Devices.Device1) == ShareCentralIO.Devices.Device1;
            bool device2Status = (status & ShareCentralIO.Devices.Device2) == ShareCentralIO.Devices.Device2;

            if(device1Status == device2Status)
                status = usbSwitch.SwitchDevices(ShareCentralIO.Devices.Device1 | ShareCentralIO.Devices.Device2);
            else if (device1Status)
                status = usbSwitch.SwitchDevices(ShareCentralIO.Devices.Device2);
            else
                status = usbSwitch.SwitchDevices(ShareCentralIO.Devices.Device1);

           status = usbSwitch.GetStatusOfDevices();

            Console.WriteLine(
                "Status 0x{0}, {1}, {2}, {3}, {4}",
                ((int)status).ToString("X2"),
                (status & ShareCentralIO.Devices.Device1) == ShareCentralIO.Devices.Device1,
                (status & ShareCentralIO.Devices.Device2) == ShareCentralIO.Devices.Device2,
                (status & ShareCentralIO.Devices.Device3) == ShareCentralIO.Devices.Device3,
                (status & ShareCentralIO.Devices.Device4) == ShareCentralIO.Devices.Device4);

            usbSwitch.Close();
        }

        private bool EnumMonitors(IntPtr hMonitor, IntPtr hdcMonitor, ref DisplayDDC.Rect lprcMonitor, IntPtr dwData)
        {
            int physicalMonitorCount = 0;
            DisplayDDC.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitorCount);

            DisplayDDC.Physical_Monitor[] physicalMonitors;
            DisplayDDC.GetPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitors);

            for(int i = 0; i < physicalMonitors.Length; ++i) {
                bool success = DisplayDDC.SetVCPFeature(physicalMonitors[i].hPhysicalMonitor, 0x60, 0x01);
            }

            IntPtr physicalMonitorsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(physicalMonitors[0]) * physicalMonitors.Length);
            Marshal.StructureToPtr(physicalMonitors[0], physicalMonitorsPtr, false);
            DisplayDDC.DestroyPhysicalMonitors(physicalMonitors.Length, physicalMonitorsPtr);
            Marshal.FreeHGlobal(physicalMonitorsPtr);
            
            return true;
        }
    }
}
