using Promptuarium.Serializer;

namespace PdfBooklet.Configuration;

[PromptuariumComment("The exported PDF")]
public sealed class OutputSettings
{
    [PromptuariumComment("The version in the PDF header: Pdf14, Pdf15, Pdf16 or Pdf17")]
    public PdfVersion PdfVersion { get; set; } = PdfVersion.Pdf17;

    [PromptuariumComment("Compress the content streams of the exported PDF")]
    public bool CompressStreams { get; set; } = true;

    /// <summary>Order the sides for a single-sided printer: all fronts, then all backs in reverse.</summary>
    [PromptuariumComment("false: front and back of every sheet alternate (duplex printers); true: all fronts, then all backs in reverse order (single-sided printers)")]
    public bool ManualDuplex { get; set; }

    /// <summary>Open the booklet in a PDF viewer once it is exported.</summary>
    [PromptuariumComment("Open the booklet in a PDF viewer once it is exported (to print it from there)")]
    public bool OpenAfterExport { get; set; } = true;

    /// <summary>The viewer the booklet is opened in: an executable or macOS .app bundle; null for the system default.</summary>
    [PromptuariumComment("The PDF viewer the exported booklet is opened in: full path of the executable (macOS: of the .app bundle); NULL: the system's default PDF application")]
    public string? Viewer { get; set; }

    public OutputSettings Clone() => new()
    {
        PdfVersion = PdfVersion,
        CompressStreams = CompressStreams,
        ManualDuplex = ManualDuplex,
        OpenAfterExport = OpenAfterExport,
        Viewer = Viewer,
    };
}
