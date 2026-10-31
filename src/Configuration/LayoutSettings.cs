using Promptuarium.Serializer;

namespace PdfBooklet.Configuration;

[PromptuariumComment("How the pages are placed on the sheet")]
public sealed class LayoutSettings
{
    [PromptuariumComment("Left (left-to-right reading) or Right (right-to-left reading): the side of the spine on the first page")]
    public BindingEdge Binding { get; set; } = BindingEdge.Left;

    [PromptuariumComment("The top margin, in mm (0 or more)")]
    public double MarginTopMm { get; set; } = 10;

    [PromptuariumComment("The bottom margin, in mm (0 or more)")]
    public double MarginBottomMm { get; set; } = 10;

    /// <summary>Margin between each page and the fold.</summary>
    [PromptuariumComment("The margin between each page and the fold, in mm (0 or more)")]
    public double MarginInnerMm { get; set; } = 10;

    /// <summary>Margin between each page and the outer edge of the sheet.</summary>
    [PromptuariumComment("The margin between each page and the outer edge of the sheet, in mm (0 or more)")]
    public double MarginOuterMm { get; set; } = 10;

    [PromptuariumComment("FitToArea (fill the area inside the margins), ActualSize (100 %) or Custom (CustomScalePercent)")]
    public ScaleMode ScaleMode { get; set; } = ScaleMode.FitToArea;

    [PromptuariumComment("The scale of the Custom scale mode, in percent (1 to 1000)")]
    public double CustomScalePercent { get; set; } = 100;

    [PromptuariumComment("Inner (towards the spine), Center or Outer (towards the outer edge); mirrored on the two pages of a spread")]
    public PageHorizontalAlignment HorizontalAlignment { get; set; } = PageHorizontalAlignment.Center;

    [PromptuariumComment("Top, Middle or Bottom")]
    public PageVerticalAlignment VerticalAlignment { get; set; } = PageVerticalAlignment.Middle;

    /// <summary>
    /// Source pages per nested signature, a multiple of 4. Zero folds the whole document
    /// as a single signature.
    /// </summary>
    [PromptuariumComment("Source pages per separately folded signature, a multiple of 4; 0 folds the whole document as one signature")]
    public int PagesPerSignature { get; set; }

    public LayoutSettings Clone() => new()
    {
        Binding = Binding,
        MarginTopMm = MarginTopMm,
        MarginBottomMm = MarginBottomMm,
        MarginInnerMm = MarginInnerMm,
        MarginOuterMm = MarginOuterMm,
        ScaleMode = ScaleMode,
        CustomScalePercent = CustomScalePercent,
        HorizontalAlignment = HorizontalAlignment,
        VerticalAlignment = VerticalAlignment,
        PagesPerSignature = PagesPerSignature,
    };
}
