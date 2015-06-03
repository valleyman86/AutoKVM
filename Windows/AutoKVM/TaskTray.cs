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
        private List<int[]> supportedMonitorSources;

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

            for (int monitorIndex = 0; monitorIndex < supportedMonitorSources.Count; ++monitorIndex)
            {
                ToolStripMenuItem monitor = new ToolStripMenuItem(String.Format("Monitor {0}", monitorIndex + 1));
                monitor.DropDown.Closing += new ToolStripDropDownClosingEventHandler(MonitorDropdownClosing);
                monitorMenus.Add(monitor);
                contextMenuStrip1.Items.Insert(contextMenuStrip1.Items.Count - 1, monitor);

                for (int sourceIndex = 0; sourceIndex < supportedMonitorSources[monitorIndex].Length; ++sourceIndex)
                {
                    ToolStripMenuItem source = new ToolStripMenuItem(String.Format("Source {0}", sourceIndex + 1));
                    source.CheckOnClick = true;
                    monitor.DropDownItems.Add(source);
                }
            }
        }

        private void InitTimer()
        {
            doublePressTimer = new System.Timers.Timer(300);
            doublePressTimer.AutoReset = false;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                doublePressTimer.Interval *= 10;
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
                        int source = supportedMonitorSources[monitorIndex][sourceIndex];
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
