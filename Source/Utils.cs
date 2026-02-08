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
                var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key == null) return false;
                
                // Try to get the build number for a more accurate check
                string? currentBuildStr = (string?)key.GetValue("CurrentBuild");
                if (int.TryParse(currentBuildStr, out int currentBuild))
                {
                    // Windows 11 starts at build 22000
                    return currentBuild >= 22000;
                }
                
                // Fallback to product name check
                string? productName = (string?)key.GetValue("ProductName");
                if (productName == null) return false;
                
                // Check for Windows 11 or any higher version number
                if (productName.StartsWith("Windows 11")) return true;
                
                // Check for potential future versions (Windows 12, 13, etc.)
                if (productName.StartsWith("Windows "))
                {
                    string versionPart = productName.Substring(8).Split(' ')[0];
                    if (int.TryParse(versionPart, out int version))
                    {
                        return version >= 11;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
