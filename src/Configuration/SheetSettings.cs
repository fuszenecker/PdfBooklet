using Promptuarium.Serializer;

namespace PdfBooklet.Configuration;

/// <summary>
/// The physical sheet each booklet side is printed on. A booklet sheet is always landscape
/// (two pages side by side, folded across the short axis), so only the two dimensions
/// matter, not their order.
/// </summary>
[PromptuariumComment("The sheet the booklet is printed on; always landscape, two pages side by side, folded across the short axis")]
public sealed class SheetSettings
{
    [PromptuariumComment("A3, A4, A5, Letter, Legal, Tabloid or Custom (CustomWidthMm x CustomHeightMm)")]
    public PaperSize PaperSize { get; set; } = PaperSize.A4;

    [PromptuariumComment("The width of a Custom sheet, in mm (50 to 2000); the order of width and height does not matter")]
    public double CustomWidthMm { get; set; } = 297;

    [PromptuariumComment("The height of a Custom sheet, in mm (50 to 2000)")]
    public double CustomHeightMm { get; set; } = 210;

    public SheetSettings Clone() => new()
    {
        PaperSize = PaperSize,
        CustomWidthMm = CustomWidthMm,
        CustomHeightMm = CustomHeightMm,
    };
}
