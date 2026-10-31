using System.Collections.Generic;
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
    /// <param name="pages">
    /// Zero-based source page indices to impose, in booklet order (see <see cref="IPageRangeParser"/>);
    /// null imposes every page of the source.
    /// </param>
    BookletPlan Plan(PdfSourceInfo source, SheetSettings sheet, LayoutSettings layout, IReadOnlyList<int>? pages = null);
}
