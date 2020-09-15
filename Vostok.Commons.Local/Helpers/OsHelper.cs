using System;
using System.Runtime.InteropServices;
using Vostok.Commons.Helpers.Windows;

namespace Vostok.Commons.Local.Helpers
{
    internal static class OsHelper
    {
        public static WindowsProcessKillJob TryCreateWindowsProcessKillJob()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsProcessKillJob();

            return null;
        }
    }
}