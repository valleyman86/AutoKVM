using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AutoKVM
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            InterceptKeys.Init();
            DisplayDDC.Init();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TaskTray());

            DisplayDDC.Cleanup();
            InterceptKeys.Cleanup();
        }
    }
}
