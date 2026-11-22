namespace PdfBooklet.ViewModels;

public enum PdfViewerOptionKind
{
    /// <summary>The system's default PDF application.</summary>
    SystemDefault,

    /// <summary>A viewer found on this computer.</summary>
    Installed,

    /// <summary>A program the user picked with the file dialog.</summary>
    Custom,

    /// <summary>The last entry: picks a program with the file dialog.</summary>
    Browse,
}

/// <summary>One entry of the viewer list in the settings dialog.</summary>
public sealed record PdfViewerOption(PdfViewerOptionKind Kind, string DisplayName, string? Path)
{
    public static PdfViewerOption SystemDefault { get; } = new(PdfViewerOptionKind.SystemDefault, "System default", null);

    public static PdfViewerOption Browse { get; } = new(PdfViewerOptionKind.Browse, "Custom…", null);

    public static PdfViewerOption Custom(string path) =>
        new(PdfViewerOptionKind.Custom, $"{System.IO.Path.GetFileName(path.TrimEnd('/', '\\'))} (custom)", path);

    /// <summary>The full path for the tooltip, or the display name when there is none.</summary>
    public string Description => Path ?? DisplayName;
}
