using System;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace PingoMeter
{
    public static class Utils
    {
        /// <summary>
        /// Return true if app running on Windows 8 or next versions.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static bool IsWindows8Next()
        {
            try
            {
                string? productName = (string?)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")?.GetValue("ProductName");
                if (productName == null) return false;
                return productName.StartsWith("Windows 8") || 
                       productName.StartsWith("Windows 10") || 
                       productName.StartsWith("Windows 11");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Return true if app running on Windows 11 or later.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static bool IsWindows11OrLater()
        {
            try
            {
                string? productName = (string?)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")?.GetValue("ProductName");
                if (productName == null) return false;
                return productName.StartsWith("Windows 11");
            }
            catch
            {
                return false;
            }
        }
    }
}
