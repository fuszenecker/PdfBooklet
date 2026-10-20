namespace PdfBooklet.Configuration;

/// <summary>
/// The physical sheet each booklet side is printed on. A booklet sheet is always landscape
/// (two pages side by side, folded across the short axis), so only the two dimensions
/// matter, not their order.
/// </summary>
public sealed class SheetSettings
{
    public PaperSize PaperSize { get; set; } = PaperSize.A4;
    public double CustomWidthMm { get; set; } = 297;
    public double CustomHeightMm { get; set; } = 210;

    public SheetSettings Clone() => new()
    {
        PaperSize = PaperSize,
        CustomWidthMm = CustomWidthMm,
        CustomHeightMm = CustomHeightMm,
    };
}
