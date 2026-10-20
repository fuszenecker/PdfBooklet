namespace PdfBooklet.Configuration;

public sealed class OutputSettings
{
    public PdfVersion PdfVersion { get; set; } = PdfVersion.Pdf17;
    public bool CompressStreams { get; set; } = true;

    public OutputSettings Clone() => new()
    {
        PdfVersion = PdfVersion,
        CompressStreams = CompressStreams,
    };
}
