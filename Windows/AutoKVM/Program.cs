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

            using (new TaskTray()) // Dispose() call when done using cleans up the task tray icon, which is otherwise visible until mouse hover.
            {
                Application.Run();
            }

            DisplayDDC.Cleanup();
            InterceptKeys.Cleanup();
        }
    }
}
