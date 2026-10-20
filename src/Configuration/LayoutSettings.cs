namespace PdfBooklet.Configuration;

public sealed class LayoutSettings
{
    public BindingEdge Binding { get; set; } = BindingEdge.Left;

    public double MarginTopMm { get; set; } = 10;
    public double MarginBottomMm { get; set; } = 10;

    /// <summary>Margin between each page and the fold.</summary>
    public double MarginInnerMm { get; set; } = 10;

    /// <summary>Margin between each page and the outer edge of the sheet.</summary>
    public double MarginOuterMm { get; set; } = 10;

    public ScaleMode ScaleMode { get; set; } = ScaleMode.FitToArea;
    public double CustomScalePercent { get; set; } = 100;

    public PageHorizontalAlignment HorizontalAlignment { get; set; } = PageHorizontalAlignment.Center;
    public PageVerticalAlignment VerticalAlignment { get; set; } = PageVerticalAlignment.Middle;

    /// <summary>
    /// Source pages per nested signature, a multiple of 4. Zero folds the whole document
    /// as a single signature.
    /// </summary>
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
