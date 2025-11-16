using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Aegis.Core.Security;

/// <summary>
/// Detects if the application is running with elevated privileges
/// Windows: Administrator detection
/// Android: Root detection (via platform-specific implementation)
/// </summary>
public static class RootDetection
{
    /// <summary>
    /// Check if running with elevated privileges
    /// </summary>
    public static bool IsElevated()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return IsWindowsAdministrator();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return IsLinuxRoot();
        }

        return false;
    }

    /// <summary>
    /// Check if current Windows user is Administrator
    /// </summary>
    private static bool IsWindowsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if running as root on Linux/Android
    /// </summary>
    private static bool IsLinuxRoot()
    {
        try
        {
            // Check for common su binaries
            string[] suPaths = new[]
            {
                "/system/app/Superuser.apk",
                "/sbin/su",
                "/system/bin/su",
                "/system/xbin/su",
                "/data/local/xbin/su",
                "/data/local/bin/su",
                "/system/sd/xbin/su",
                "/system/bin/failsafe/su",
                "/data/local/su",
                "/su/bin/su"
            };

            foreach (var path in suPaths)
            {
                if (File.Exists(path))
                    return true;
            }

            // Check if we're running as UID 0
            return Environment.UserName == "root";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check for common root management apps (Android)
    /// </summary>
    public static bool HasRootManagementApps()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return false;

        string[] rootApps = new[]
        {
            "com.noshufou.android.su",
            "com.thirdparty.superuser",
            "eu.chainfire.supersu",
            "com.koushikdutta.superuser",
            "com.zachspong.temprootremovejb",
            "com.ramdroid.appquarantine",
            "com.topjohnwu.magisk"
        };

        // This would require Android-specific implementation
        // For now, return false on non-Android platforms
        return false;
    }

    /// <summary>
    /// Perform comprehensive security check
    /// </summary>
    public static SecurityCheckResult PerformSecurityCheck()
    {
        return new SecurityCheckResult
        {
            IsElevated = IsElevated(),
            IsDebuggerAttached = Debugger.IsAttached,
            HasRootAccess = HasRootManagementApps(),
            Platform = GetPlatform()
        };
    }

    private static string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux/Android";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";

        return "Unknown";
    }
}

/// <summary>
/// Security check result
/// </summary>
public class SecurityCheckResult
{
    public bool IsElevated { get; set; }
    public bool IsDebuggerAttached { get; set; }
    public bool HasRootAccess { get; set; }
    public string Platform { get; set; } = string.Empty;

    public bool IsSafe => !IsElevated && !IsDebuggerAttached && !HasRootAccess;
}
