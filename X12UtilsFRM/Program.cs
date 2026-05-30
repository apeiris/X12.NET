using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using NLog.Config;

namespace X12UtilsFRM
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                FunctoidRegistry.LoadRegistry();  
                Application.Run(new X12UtilsFRM());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Registry initialization failed early:\n{ex.Message}", "Init Error");
            }
         
        }
    }
}
