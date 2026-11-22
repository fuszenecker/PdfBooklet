using System;
using System.Reflection;
using System.Runtime.InteropServices;
using PdfBooklet.Interfaces;

namespace PdfBooklet.Services;

/// <summary>Reads the version and copyright from the application's assembly attributes (set from <c>version.txt</c>).</summary>
public sealed class ApplicationInfo : IApplicationInfo
{
    public ApplicationInfo()
        : this(typeof(ApplicationInfo).Assembly)
    {
    }

    internal ApplicationInfo(Assembly assembly)
    {
        Version = ParseVersion(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            assembly.GetName().Version);
        ShortVersion = Shorten(Version);
        Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        Runtime = $"{RuntimeInformation.FrameworkDescription}, {RuntimeInformation.RuntimeIdentifier}";
    }

    public string Name => "PdfBooklet";

    public string Version { get; }

    public string ShortVersion { get; }

    public string Copyright { get; }

    public string Runtime { get; }

    /// <summary>The informational version without the source revision the SDK appends ("1.3.0.0+abc123").</summary>
    internal static string ParseVersion(string? informationalVersion, Version? assemblyVersion)
    {
        var version = informationalVersion?.Split('+', 2)[0].Trim();
        return string.IsNullOrEmpty(version) ? assemblyVersion?.ToString() ?? "0.0.0.0" : version;
    }

    /// <summary>"1.3.0.0" → "1.3.0"; anything that is not a four-part version is kept as it is.</summary>
    internal static string Shorten(string version) =>
        System.Version.TryParse(version, out var parsed) && parsed.Revision >= 0 ? parsed.ToString(3) : version;
}
