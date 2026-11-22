using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using PdfBooklet.Exceptions;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;

namespace PdfBooklet.Services;

/// <summary>
/// Starts a PDF viewer on a file: the chosen executable with the file as its only argument (a macOS <c>.app</c> bundle
/// through <c>open -a</c>), or the system default through the shell (Windows), <c>open</c> (macOS) or <c>xdg-open</c>.
/// </summary>
public sealed class PdfViewerLauncher : IPdfViewerLauncher
{
    private readonly HostPlatform _platform;
    private readonly Func<ProcessStartInfo, IDisposable?> _start;

    public PdfViewerLauncher()
        : this(HostPlatforms.Current, Process.Start)
    {
    }

    internal PdfViewerLauncher(HostPlatform platform, Func<ProcessStartInfo, IDisposable?> start)
    {
        _platform = platform;
        _start = start;
    }

    public void Open(string pdfPath, string? viewer)
    {
        var startInfo = CreateStartInfo(_platform, pdfPath, viewer);
        var application = string.IsNullOrWhiteSpace(viewer) ? "the default PDF application" : Path.GetFileName(viewer.TrimEnd('/', '\\'));

        try
        {
            // The viewer runs on its own; the handle is not needed.
            _start(startInfo)?.Dispose();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            throw new PdfBookletException($"Could not start {application}: {ex.Message}", ex);
        }
    }

    internal static ProcessStartInfo CreateStartInfo(HostPlatform platform, string pdfPath, string? viewer)
    {
        if (string.IsNullOrWhiteSpace(viewer))
        {
            return platform switch
            {
                HostPlatform.Windows => new ProcessStartInfo(pdfPath) { UseShellExecute = true },
                HostPlatform.MacOS => new ProcessStartInfo("open") { ArgumentList = { pdfPath } },
                _ => new ProcessStartInfo("xdg-open") { ArgumentList = { pdfPath } },
            };
        }

        if (platform == HostPlatform.MacOS && viewer.TrimEnd('/').EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        {
            return new ProcessStartInfo("open") { ArgumentList = { "-a", viewer, pdfPath } };
        }

        return new ProcessStartInfo(viewer) { ArgumentList = { pdfPath }, UseShellExecute = false };
    }
}
