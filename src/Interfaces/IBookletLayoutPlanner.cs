using PdfBooklet.Configuration;
using PdfBooklet.Models;

namespace PdfBooklet.Interfaces;

public interface IBookletLayoutPlanner
{
    /// <summary>
    /// Imposes the source pages onto sheets: which page goes where on every sheet side, and
    /// at what scale and offset. Pure computation shared by the preview and the exporter.
    /// Settings must already have passed <see cref="IBookletSettingsValidator"/>.
    /// </summary>
    BookletPlan Plan(PdfSourceInfo source, SheetSettings sheet, LayoutSettings layout);
}
