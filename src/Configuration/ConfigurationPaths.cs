using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PdfBooklet.Configuration;

public static class ConfigurationPaths
{
    public static string[] GetPerUserConfigPaths() => GetPerUserConfigPaths(
        CurrentPlatform(),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"));

    internal static string[] GetPerUserConfigPaths(
        OSPlatform platform, string home, string roaming, string local, string? xdgConfigHome)
    {
        if (platform == OSPlatform.Windows)
        {
            return
            [
                Path.Combine(roaming, "PdfBooklet", "appsettings.json"),
                Path.Combine(local, "PdfBooklet", "appsettings.json")
            ];
        }

        if (platform == OSPlatform.Linux)
        {
            var configRoot = !string.IsNullOrWhiteSpace(xdgConfigHome)
                ? xdgConfigHome
                : Path.Combine(home, ".config");

            return
            [
                Path.Combine(configRoot, "PdfBooklet", "appsettings.json")
            ];
        }

        if (platform == OSPlatform.OSX)
        {
            return
            [
                Path.Combine(home, "Library", "Application Support", "PdfBooklet", "appsettings.json")
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

    private static OSPlatform CurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSPlatform.Linux;
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX : OSPlatform.Create("OTHER");
    }
}
