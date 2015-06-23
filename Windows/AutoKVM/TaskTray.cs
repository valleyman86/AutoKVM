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
    }
}
