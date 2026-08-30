using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace iReverse_Unisoc_Ultimate
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string logPath = Path.Combine(Path.GetTempPath(), "iReverse_Startup.log");
            try
            {
                File.WriteAllText(logPath, "=== iReverse Startup Log ===\n");
                File.AppendAllText(logPath, DateTime.Now.ToString() + " - Starting...\n");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                File.AppendAllText(logPath, DateTime.Now.ToString() + " - Creating Main form...\n");
                var mainForm = new Main();

                File.AppendAllText(logPath, DateTime.Now.ToString() + " - Running application...\n");
                Application.Run(mainForm);

                File.AppendAllText(logPath, DateTime.Now.ToString() + " - Application exited normally.\n");
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(logPath, DateTime.Now.ToString() + " - FATAL ERROR:\n");
                    File.AppendAllText(logPath, ex.ToString() + "\n");
                    if (ex.InnerException != null)
                    {
                        File.AppendAllText(logPath, "INNER EXCEPTION:\n");
                        File.AppendAllText(logPath, ex.InnerException.ToString() + "\n");
                    }
                }
                catch { }

                try
                {
                    MessageBox.Show(
                        "Failed to start application.\n\nError: " + ex.Message + "\n\nCheck log at:\n" + logPath,
                        "iReverse Unisoc Ultimate - Startup Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                catch { }
            }
        }
    }
}
