using System;

namespace PdfBooklet.Models;

/// <summary>The desktop platforms PdfBooklet runs on, as far as launching other applications is concerned.</summary>
public enum HostPlatform
{
    Linux,
    Windows,
    MacOS,
}

public static class HostPlatforms
{
    public static HostPlatform Current { get; } =
        OperatingSystem.IsWindows() ? HostPlatform.Windows
        : OperatingSystem.IsMacOS() ? HostPlatform.MacOS
        : HostPlatform.Linux;
}
