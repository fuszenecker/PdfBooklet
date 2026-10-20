namespace PdfBooklet.Configuration;

public sealed class AppConfiguration
{
    public SheetSettings Sheet { get; set; } = new();
    public LayoutSettings Layout { get; set; } = new();
    public OutputSettings Output { get; set; } = new();
    public UiPreferences UiPreferences { get; set; } = new();

    /// <summary>
    /// Produces an independent deep copy of the entire configuration graph.
    /// This is the single authority for copying configuration; callers must not
    /// hand-copy individual properties.
    /// </summary>
    public AppConfiguration Clone() => new()
    {
        Sheet = (Sheet ?? new SheetSettings()).Clone(),
        Layout = (Layout ?? new LayoutSettings()).Clone(),
        Output = (Output ?? new OutputSettings()).Clone(),
        UiPreferences = (UiPreferences ?? new UiPreferences()).Clone(),
    };
}
