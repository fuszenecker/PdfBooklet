using Promptuarium.Serializer;

namespace PdfBooklet.Configuration;

[PromptuariumComment("Remembered by the user interface")]
public sealed class UiPreferences
{
    [PromptuariumComment("The folder of the last imported PDF (NULL: none yet)")]
    public string? LastSourceDirectory { get; set; }

    [PromptuariumComment("The folder of the last exported booklet (NULL: none yet)")]
    public string? LastExportDirectory { get; set; }

    public UiPreferences Clone() => new()
    {
        LastSourceDirectory = LastSourceDirectory,
        LastExportDirectory = LastExportDirectory,
    };
}
