using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Aegis.Core.Security;

/// <summary>
/// Anti-debugging mechanisms to detect if application is being debugged
/// </summary>
public static class AntiDebug
{
    /// <summary>
    /// Check if a debugger is attached
    /// </summary>
    public static bool IsDebuggerAttached()
    {
        return Debugger.IsAttached;
    }

    /// <summary>
    /// Check if debugger is present (Windows-specific)
    /// </summary>
    public static bool IsDebuggerPresent()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return NativeMethods.IsDebuggerPresent();
        }

        return Debugger.IsAttached;
    }

    /// <summary>
    /// Perform timing check to detect debugger
    /// Debuggers slow down execution
    /// </summary>
    public static bool TimingCheck()
    {
        var sw = Stopwatch.StartNew();

        // Simple operation that should be fast
        int dummy = 0;
        for (int i = 0; i < 1000; i++)
        {
            dummy += i;
        }

        sw.Stop();

        // If execution is too slow, might be debugged
        return sw.ElapsedMilliseconds > 100;
    }

    /// <summary>
    /// Perform comprehensive anti-debug check
    /// </summary>
    public static AntiDebugResult PerformCheck()
    {
        return new AntiDebugResult
        {
            DebuggerAttached = IsDebuggerAttached(),
            DebuggerPresent = IsDebuggerPresent(),
            TimingAnomalyDetected = TimingCheck()
        };
    }
}

/// <summary>
/// Anti-debug check result
/// </summary>
public class AntiDebugResult
{
    public bool DebuggerAttached { get; set; }
    public bool DebuggerPresent { get; set; }
    public bool TimingAnomalyDetected { get; set; }

    public bool IsDebugDetected => DebuggerAttached || DebuggerPresent || TimingAnomalyDetected;
}

/// <summary>
/// Native Windows methods
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsDebuggerPresent();
}
