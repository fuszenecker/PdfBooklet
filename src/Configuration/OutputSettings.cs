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

    public OutputSettings Clone() => new()
    {
        PdfVersion = PdfVersion,
        CompressStreams = CompressStreams,
        ManualDuplex = ManualDuplex,
    };
}
