using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Diagnostics;

namespace Plexity.Helpers
{
    public static class WindowsVersionHelper
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_DEFAULT = 0;
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWCP_ROUNDSMALL = 3;

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        public static bool IsWindows11OrGreater()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    var currentBuild = key.GetValue("CurrentBuild") as string;
                    if (!string.IsNullOrEmpty(currentBuild) && int.TryParse(currentBuild, out int buildNumber))
                    {
                        // Windows 11 starts at build 22000
                        return buildNumber >= 22000;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking Windows version: {ex.Message}");
            }
            
            return false;
        }

        public static bool IsWindows11OrNewer()
        {
            var osVersionInfo = new OSVERSIONINFOEX
            {
                dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX))
            };
            
            if (RtlGetVersion(ref osVersionInfo) != 0)
                return false;

            // Windows 11 is Windows 10 with build number >= 22000
            return osVersionInfo.dwMajorVersion >= 10 && osVersionInfo.dwBuildNumber >= 22000;
        }

        public static Version GetWindowsVersion()
        {
            var osVersionInfo = new OSVERSIONINFOEX
            {
                dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX))
            };
            
            if (RtlGetVersion(ref osVersionInfo) != 0)
                return new Version(0, 0);

            return new Version(osVersionInfo.dwMajorVersion, osVersionInfo.dwMinorVersion, osVersionInfo.dwBuildNumber);
        }

        public static void ApplyRoundedCorners(Window window, bool forceEnable = false)
        {
            try
            {
                if (forceEnable || IsWindows11OrGreater())
                {
                    var hwnd = new WindowInteropHelper(window).EnsureHandle();
                    int preference = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply rounded corners: {ex.Message}");
            }
        }
    }
}