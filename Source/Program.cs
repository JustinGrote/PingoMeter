using System.Diagnostics;

namespace PingoMeter
{
    internal static class Program
    {
        /// <summary> x.x.x program version string. </summary>
        public const string VERSION = "1.0.0-alpha1";

        [STAThread]
        public static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                // Warning!
                // Do not use the debugger. This can cause a BSOD.
                // This is a known bug in Windows 7, you'll get a BSOD with bug-check code 0x76...
                // More: https://stackoverflow.com/questions/17756824/blue-screen-when-using-ping
                //Debugger.Break();
                //return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Create and run the notification icon
                using var notificationIcon = new NotificationIcon();
                notificationIcon.Run();
            }
            catch (Exception ex)
            {
                File.WriteAllText("error.txt", "[PingoMeter crash log]\n\n" + ex.ToString());
                Process.Start("error.txt");
            }
        }
    }
}
