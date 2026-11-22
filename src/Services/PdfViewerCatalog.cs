using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;

namespace PdfBooklet.Services;

/// <summary>
/// Looks for well-known PDF viewers: on Linux by executable name on the <c>PATH</c>, on Windows at their usual
/// install locations, on macOS as application bundles. The search runs once, on first use.
/// </summary>
public sealed class PdfViewerCatalog : IPdfViewerCatalog
{
    private static readonly (string Name, string[] Executables)[] LinuxViewers =
    [
        ("Papers", ["papers"]),
        ("Evince", ["evince"]),
        ("Okular", ["okular"]),
        ("Atril", ["atril"]),
        ("Xreader", ["xreader"]),
        ("Zathura", ["zathura"]),
        ("qpdfview", ["qpdfview"]),
        ("MuPDF", ["mupdf", "mupdf-gl", "mupdf-x11"]),
        ("Xpdf", ["xpdf"]),
        ("Firefox", ["firefox"]),
        ("Chromium", ["chromium", "chromium-browser"]),
        ("Google Chrome", ["google-chrome", "google-chrome-stable"]),
    ];

    // Paths relative to %ProgramFiles%, %ProgramFiles(x86)% or %LocalAppData%.
    private static readonly (string Name, string[] RelativePaths)[] WindowsViewers =
    [
        ("Adobe Acrobat", [@"Adobe\Acrobat DC\Acrobat\Acrobat.exe"]),
        ("Adobe Acrobat Reader", [@"Adobe\Acrobat Reader DC\Reader\AcroRd32.exe", @"Adobe\Acrobat Reader\Reader\AcroRd32.exe"]),
        ("SumatraPDF", [@"SumatraPDF\SumatraPDF.exe"]),
        ("Foxit PDF Reader", [@"Foxit Software\Foxit PDF Reader\FoxitPDFReader.exe"]),
        ("PDF-XChange Editor", [@"Tracker Software\PDF Editor\PDFXEdit.exe"]),
        ("Microsoft Edge", [@"Microsoft\Edge\Application\msedge.exe"]),
        ("Firefox", [@"Mozilla Firefox\firefox.exe"]),
        ("Google Chrome", [@"Google\Chrome\Application\chrome.exe"]),
    ];

    private static readonly (string Name, string Bundle)[] MacViewers =
    [
        ("Preview", "Preview.app"),
        ("Adobe Acrobat Reader", "Adobe Acrobat Reader.app"),
        ("Adobe Acrobat Reader DC", "Adobe Acrobat Reader DC.app"),
        ("Skim", "Skim.app"),
        ("PDF Expert", "PDF Expert.app"),
        ("Safari", "Safari.app"),
        ("Firefox", "Firefox.app"),
        ("Google Chrome", "Google Chrome.app"),
    ];

    private readonly Lazy<IReadOnlyList<PdfViewer>> _installed;

    public PdfViewerCatalog()
        : this(HostPlatforms.Current, Environment.GetEnvironmentVariable, path => File.Exists(path) || Directory.Exists(path))
    {
    }

    internal PdfViewerCatalog(HostPlatform platform, Func<string, string?> getEnvironmentVariable, Func<string, bool> exists)
    {
        _installed = new Lazy<IReadOnlyList<PdfViewer>>(() => Find(platform, getEnvironmentVariable, exists));
    }

    public IReadOnlyList<PdfViewer> InstalledViewers => _installed.Value;

    internal static IReadOnlyList<PdfViewer> Find(HostPlatform platform, Func<string, string?> getEnvironmentVariable, Func<string, bool> exists) =>
        platform switch
        {
            HostPlatform.Windows => FindWindows(getEnvironmentVariable, exists),
            HostPlatform.MacOS => FindMac(getEnvironmentVariable, exists),
            _ => FindLinux(getEnvironmentVariable, exists),
        };

    private static List<PdfViewer> FindLinux(Func<string, string?> getEnvironmentVariable, Func<string, bool> exists)
    {
        var directories = (getEnvironmentVariable("PATH") ?? string.Empty)
            .Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var found = new List<PdfViewer>();
        foreach (var (name, executables) in LinuxViewers)
        {
            if (FirstExisting(directories, executables, (directory, executable) => directory + "/" + executable, exists) is { } path)
            {
                found.Add(new PdfViewer(name, path));
            }
        }

        return found;
    }

    private static List<PdfViewer> FindWindows(Func<string, string?> getEnvironmentVariable, Func<string, bool> exists)
    {
        var roots = new List<string>();
        foreach (var variable in new[] { "ProgramFiles", "ProgramFiles(x86)", "LOCALAPPDATA" })
        {
            var root = getEnvironmentVariable(variable)?.Trim().TrimEnd('\\');
            if (!string.IsNullOrEmpty(root) && !roots.Contains(root, StringComparer.OrdinalIgnoreCase))
            {
                roots.Add(root);
            }
        }

        var found = new List<PdfViewer>();
        foreach (var (name, relativePaths) in WindowsViewers)
        {
            if (FirstExisting(roots, relativePaths, (root, relative) => root + "\\" + relative, exists) is { } path)
            {
                found.Add(new PdfViewer(name, path));
            }
        }

        return found;
    }

    private static List<PdfViewer> FindMac(Func<string, string?> getEnvironmentVariable, Func<string, bool> exists)
    {
        var roots = new List<string> { "/System/Applications", "/Applications", "/System/Volumes/Preboot/Cryptexes/App/System/Applications" };
        if (getEnvironmentVariable("HOME") is { Length: > 0 } home)
        {
            roots.Add(home.TrimEnd('/') + "/Applications");
        }

        var found = new List<PdfViewer>();
        foreach (var (name, bundle) in MacViewers)
        {
            if (FirstExisting(roots, [bundle], (root, b) => root + "/" + b, exists) is { } path)
            {
                found.Add(new PdfViewer(name, path));
            }
        }

        return found;
    }

    private static string? FirstExisting(
        IEnumerable<string> roots, string[] names, Func<string, string, string> combine, Func<string, bool> exists)
    {
        // Names first: the preferred executable name wins over a fallback found earlier on the path.
        foreach (var name in names)
        {
            foreach (var root in roots)
            {
                var candidate = combine(root, name);
                if (exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
