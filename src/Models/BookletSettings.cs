using PdfBooklet.Configuration;

namespace PdfBooklet.Models;

/// <summary>The three editable settings sections the settings dialog edits as one unit.</summary>
public sealed record BookletSettings(SheetSettings Sheet, LayoutSettings Layout, OutputSettings Output);
