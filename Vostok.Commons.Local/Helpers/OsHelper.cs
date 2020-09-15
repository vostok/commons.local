using System;
using System.Runtime.InteropServices;
using Vostok.Commons.Helpers.Windows;

namespace Vostok.Commons.Local.Helpers
{
    internal static class OsHelper
    {
        public static string GetCommandLineName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "cmd";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "/bin/bash";

            throw new Exception($"Unknown platform {RuntimeInformation.OSDescription}.");
        }

        public static string GetCommandLineArguments()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return " /D /C ";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return " -lc ";

            throw new Exception($"Unknown platform {RuntimeInformation.OSDescription}.");
        }

        public static WindowsProcessKillJob TryCreateWindowsProcessKillJob()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsProcessKillJob();

            return null;
        }
    }
}