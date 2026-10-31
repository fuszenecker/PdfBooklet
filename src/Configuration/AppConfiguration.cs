using Promptuarium.Serializer;

namespace PdfBooklet.Configuration;

[PromptuariumComment("PdfBooklet settings: the bundled settings.prmt holds every default, the per-user settings.prmt is applied on top of it")]
public sealed class AppConfiguration
{
    [PromptuariumComment("The sheet the booklet is printed on")]
    public SheetSettings Sheet { get; set; } = new();

    [PromptuariumComment("How the pages are placed on the sheet")]
    public LayoutSettings Layout { get; set; } = new();

    [PromptuariumComment("The exported PDF")]
    public OutputSettings Output { get; set; } = new();

    [PromptuariumComment("Remembered by the user interface")]
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
