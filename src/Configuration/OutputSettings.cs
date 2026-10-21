namespace PdfBooklet.Configuration;

public sealed class OutputSettings
{
    public PdfVersion PdfVersion { get; set; } = PdfVersion.Pdf17;
    public bool CompressStreams { get; set; } = true;

    /// <summary>Order the sides for a single-sided printer: all fronts, then all backs in reverse.</summary>
    public bool ManualDuplex { get; set; }

    public OutputSettings Clone() => new()
    {
        PdfVersion = PdfVersion,
        CompressStreams = CompressStreams,
        ManualDuplex = ManualDuplex,
    };
}
