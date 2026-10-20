using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PdfBooklet.Configuration;

public static class ConfigurationPaths
{
    public static string[] GetPerUserConfigPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return
            [
                Path.Combine(roaming, "PdfBooklet", "appsettings.json"),
                Path.Combine(local, "PdfBooklet", "appsettings.json")
            ];
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configRoot = !string.IsNullOrWhiteSpace(xdgConfigHome)
                ? xdgConfigHome
                : Path.Combine(home, ".config");

            return
            [
                Path.Combine(configRoot, "PdfBooklet", "appsettings.json")
            ];
        }

        return [];
    }

    public static string? GetPreferredPerUserConfigPath()
    {
        var paths = GetPerUserConfigPaths();
        return paths.Length > 0 ? paths[^1] : null;
    }

    /// <summary>
    /// The per-user directory where PdfBooklet stores its data (configuration,
    /// statistics database, etc.). Single source of truth so stores don't each
    /// re-derive the location independently.
    /// </summary>
    public static string GetAppDataDirectory()
    {
        var preferredConfigPath = GetPreferredPerUserConfigPath();
        var directory = preferredConfigPath is not null
            ? Path.GetDirectoryName(preferredConfigPath)
            : null;

        return string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PdfBooklet")
            : directory;
    }
}