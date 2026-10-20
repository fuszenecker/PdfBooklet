using PdfBooklet.Configuration;
using PdfBooklet.Models;

namespace PdfBooklet.Interfaces;

public interface ISheetSizeResolver
{
    /// <summary>
    /// The sheet size in points, normalized to landscape (width ≥ height) because a booklet
    /// sheet carries two pages side by side.
    /// </summary>
    PageSize Resolve(SheetSettings sheet);
}
