namespace PdfBooklet.Configuration;

public sealed class UiPreferences
{
    public string? LastSourceDirectory { get; set; }
    public string? LastExportDirectory { get; set; }

    public UiPreferences Clone() => new()
    {
        LastSourceDirectory = LastSourceDirectory,
        LastExportDirectory = LastExportDirectory,
    };
}
