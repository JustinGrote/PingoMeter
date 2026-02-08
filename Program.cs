using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PingoMeter
{
    [SupportedOSPlatform("windows")]
    internal static class Program
    {
        /// <summary> x.x.x program version string. </summary>
        public const string VERSION = "0.9.9";

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
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Build the host for dependency injection
                using var host = CreateHostBuilder(args).Build();
                
                // Get the singleton instance and run the application
                var notificationIcon = host.Services.GetRequiredService<NotificationIcon>();
                notificationIcon.Run();
                
                // Dispose the singleton after the application exits
                notificationIcon.Dispose();
            }
            catch (Exception ex)
            {
                File.WriteAllText("error.txt", "[PingoMeter crash log]\n\n" + ex.ToString());
                Process.Start("error.txt");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register application services
                    services.AddSingleton<NotificationIcon>();
                    services.AddLogging(logging =>
                    {
                        logging.AddConsole();
                        logging.AddDebug();
                    });
                });
    }
}
