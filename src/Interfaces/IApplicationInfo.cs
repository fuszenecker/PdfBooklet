namespace PdfBooklet.Interfaces;

/// <summary>What the running application knows about itself, for the window title and the About dialog.</summary>
public interface IApplicationInfo
{
    /// <summary>The application name.</summary>
    string Name { get; }

    /// <summary>The full version from <c>version.txt</c>, e.g. "1.3.0.0".</summary>
    string Version { get; }

    /// <summary>The version without the build number, as the release notes name it, e.g. "1.3.0".</summary>
    string ShortVersion { get; }

    string Copyright { get; }

    /// <summary>The .NET runtime and platform, e.g. ".NET 10.0.1, linux-x64", for bug reports.</summary>
    string Runtime { get; }
}
