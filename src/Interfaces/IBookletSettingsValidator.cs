using PdfBooklet.Configuration;

namespace PdfBooklet.Interfaces;

public interface IBookletSettingsValidator
{
    /// <summary>Returns a user-facing description of the first problem, or null when the settings are usable.</summary>
    string? Validate(SheetSettings sheet, LayoutSettings layout);
}
