using System;
using System.IO;
using Microsoft.Win32;

namespace GhostClip;

// Per-user sign-in launch. No administrator permission, task scheduler,
// service, or shell replacement is required. The --startup argument makes
// GhostClip display its widget without activating or focusing it.
internal static class WindowsStartupRegistration
{
    private const string KeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string ValueName = "GhostClip";
    private const string StartupArgument = "--startup";

    internal static bool IsStartupLaunch()
    {
        foreach (string argument in Environment.GetCommandLineArgs()[1..])
        {
            if (string.Equals(
                argument, StartupArgument, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);

            return string.Equals(
                key?.GetValue(ValueName) as string,
                GetExpectedCommand(),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static bool TrySetEnabled(bool enabled, out string? error)
    {
        error = null;

        try
        {
            if (enabled)
            {
                using RegistryKey key =
                    Registry.CurrentUser.CreateSubKey(KeyPath, writable: true)
                    ?? throw new InvalidOperationException(
                        "Unable to open your Windows startup settings.");

                key.SetValue(
                    ValueName,
                    GetExpectedCommand(),
                    RegistryValueKind.String);
            }
            else
            {
                using RegistryKey? key =
                    Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);

                key?.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            if (IsEnabled() != enabled)
            {
                throw new InvalidOperationException(
                    "Windows did not retain the requested startup setting.");
            }

            return true;
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException or
            System.Security.SecurityException or
            IOException or
            ArgumentException or
            InvalidOperationException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string GetExpectedCommand()
    {
        string executable = Environment.ProcessPath
            ?? throw new InvalidOperationException(
                "Cannot determine the GhostClip executable path.");

        if (!executable.EndsWith(
                ".exe", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(executable))
        {
            throw new InvalidOperationException(
                "Windows startup requires a GhostClip executable.");
        }

        return $"\"{executable}\" {StartupArgument}";
    }
}
