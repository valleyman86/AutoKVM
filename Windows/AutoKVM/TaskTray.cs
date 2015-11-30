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
    public partial class TaskTray : Form
    {
        private List<ToolStripMenuItem> monitorMenus;
        private List<DisplayDDC.MonitorSource[]> supportedMonitorSources;

        private System.Timers.Timer doublePressTimer;

        public TaskTray()
        {
            InitializeComponent();

            AddMonitorsAndSources();
            AddUSBSwitches();
            InitTimer();
            InterceptKeys.RegisterCallback(GlobalKeydownCallback);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.ShowInTaskbar = false; 
            this.Visible = false;
        }

        private void AddMonitorsAndSources()
        {
            monitorMenus = new List<ToolStripMenuItem>();
            supportedMonitorSources = DisplayDDC.GetMonitorSupportedSources();
            List<int> activeMonitorSources = DisplayDDC.GetMonitorActiveSources();

            for (int monitorIndex = 0; monitorIndex < supportedMonitorSources.Count; ++monitorIndex)
            {
                ToolStripMenuItem monitor = new ToolStripMenuItem(String.Format("Monitor {0}", monitorIndex + 1));
                monitor.DropDown.Closing += new ToolStripDropDownClosingEventHandler(MonitorDropdownClosing);
                monitorMenus.Add(monitor);
                contextMenuStrip1.Items.Insert(contextMenuStrip1.Items.Count - 1, monitor);

                for (int sourceIndex = 0; sourceIndex < supportedMonitorSources[monitorIndex].Length; ++sourceIndex)
                {
                    DisplayDDC.MonitorSource source = supportedMonitorSources[monitorIndex][sourceIndex];
                    ToolStripMenuItem sourceMenuItem = new ToolStripMenuItem(String.Format("{0}", source.name));
                    sourceMenuItem.CheckOnClick = true;
                    if (source.code == activeMonitorSources[monitorIndex])
                        sourceMenuItem.Checked = true;

                    monitor.DropDownItems.Add(sourceMenuItem);
                }
            }
        }

        private void InitTimer()
        {
            doublePressTimer = new System.Timers.Timer(300);
            doublePressTimer.AutoReset = false;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                doublePressTimer.Interval *= 10; // Add some time, since the debugger can eat some time up.
            }
        }

        private void GlobalKeydownCallback(Keys key)
        {
            switch (key)
            {
                //case Keys.Escape:
                //    Application.Exit();
                //    break;

                case Keys.Scroll:
                    HandleScrolllock();
                    break;

                default:
                    break;
            }
        }

        private void HandleScrolllock()
        {
            if (doublePressTimer.Enabled)
            {
                doublePressTimer.Stop();
                CycleDisplays();
            }
            else
            {
                doublePressTimer.Start();
            }
        }

        private void CycleDisplays()
        {
            for (int monitorIndex = 0; monitorIndex < supportedMonitorSources.Count; ++monitorIndex)
            {
                List<int> enabledSources = new List<int>();
                for (int sourceIndex = 0; sourceIndex < supportedMonitorSources[monitorIndex].Length; ++sourceIndex)
                {
                    ToolStripMenuItem sourceMenu = (ToolStripMenuItem)monitorMenus[monitorIndex].DropDownItems[sourceIndex];
                    if (sourceMenu.Checked)
                    {
                        int source = supportedMonitorSources[monitorIndex][sourceIndex].code;
                        enabledSources.Add(source);
                    }
                }

                DisplayDDC.CycleDisplaySources(monitorIndex, enabledSources.ToArray());
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button.Equals(MouseButtons.Left)) {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon1, null);
            }
        }

        private void switchDisplaysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CycleDisplays();
        }
        
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MonitorDropdownClosing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
                ((ToolStripDropDownMenu)sender).Invalidate();
            }
        }

        private void AddUSBSwitches()
        {
            Tuple<ushort, ushort>[] supportedDevices = new Tuple<ushort, ushort>[1] { new Tuple<ushort, ushort>(ShareCentralIO.vendorID, ShareCentralIO.productID) };

            ToolStripMenuItem usbSwitchesMenuItem = null;

            List<HIDAPI.HIDDeviceInfo> devices = HIDAPI.HIDEnumerate(0, 0);
            foreach (HIDAPI.HIDDeviceInfo device in devices) {
                if (Array.Exists(supportedDevices, supportedDevice => supportedDevice.Item1 == device.vendor_id && supportedDevice.Item2 == device.product_id)) {
                    if (usbSwitchesMenuItem == null) {
                        usbSwitchesMenuItem = new ToolStripMenuItem(String.Format("USB Switches"));
                        contextMenuStrip1.Items.Insert(contextMenuStrip1.Items.Count - 1, usbSwitchesMenuItem);
                    }

                    ToolStripMenuItem usbSwitchMenuItem = new ToolStripMenuItem(String.Format("{0}", device.product_string));
                    usbSwitchMenuItem.CheckOnClick = true;
                    usbSwitchMenuItem.DropDown.Closing += new ToolStripDropDownClosingEventHandler(MonitorDropdownClosing);
                    usbSwitchesMenuItem.DropDownItems.Add(usbSwitchMenuItem);

                    ShareCentralIO usbSwitch = new ShareCentralIO();
                    ShareCentralIO.Devices status = usbSwitch.GetStatusOfDevices();
                    bool device1Status = (status & ShareCentralIO.Devices.Device1) == ShareCentralIO.Devices.Device1;
                    bool device2Status = (status & ShareCentralIO.Devices.Device2) == ShareCentralIO.Devices.Device2;
                    bool device3Status = (status & ShareCentralIO.Devices.Device3) == ShareCentralIO.Devices.Device3;
                    bool device4Status = (status & ShareCentralIO.Devices.Device4) == ShareCentralIO.Devices.Device4;

                    int breakTest = 0;
                    //ToolStripMenuItem usbSwitch = new ToolStripMenuItem(String.Format("Monitor {0}", device.product_string));
                    //usbSwitch.DropDown.Closing += new ToolStripDropDownClosingEventHandler(MonitorDropdownClosing);
                    //monitorMenus.Add(usbSwitch);
                    //contextMenuStrip1.Items.Insert(contextMenuStrip1.Items.Count - 1, monitor);

                    //for (int sourceIndex = 0; sourceIndex < supportedMonitorSources[monitorIndex].Length; ++sourceIndex) {
                    //    DisplayDDC.MonitorSource source = supportedMonitorSources[monitorIndex][sourceIndex];
                    //    ToolStripMenuItem sourceMenuItem = new ToolStripMenuItem(String.Format("{0}", source.name));
                    //    sourceMenuItem.CheckOnClick = true;
                    //    if (source.code == activeMonitorSources[monitorIndex])
                    //        sourceMenuItem.Checked = true;

                    //    monitor.DropDownItems.Add(sourceMenuItem);
                    //}
                }

            }
        }
    }
}
