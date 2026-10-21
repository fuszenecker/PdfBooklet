using System.Collections.Generic;
using System.Linq;

namespace PdfBooklet.Models;

/// <summary>
/// One half of a sheet side. <see cref="SourcePageIndex"/> is the zero-based page of the
/// source document, or null for a blank padding page.
/// </summary>
/// <param name="HalfSheet">The half of the sheet this page occupies, clipped to when drawn.</param>
/// <param name="Content">Where the scaled page itself is drawn.</param>
/// <param name="Scale">Factor applied to the source page size.</param>
public sealed record PlacedPage(int? SourcePageIndex, PageRect HalfSheet, PageRect Content, double Scale);

public sealed record SheetSide(PlacedPage Left, PlacedPage Right);

public sealed record BookletSheet(SheetSide Front, SheetSide Back);

public sealed record BookletPlan(PageSize Sheet, IReadOnlyList<BookletSheet> Sheets)
{
    public int SideCount => Sheets.Count * 2;

    /// <summary>Sides in output-PDF order: front/back alternating, or (manual duplex) all fronts, then all backs in reverse.</summary>
    public IReadOnlyList<SheetSide> SidesInPrintOrder(bool manualDuplex) => manualDuplex
        ? [.. Sheets.Select(sheet => sheet.Front), .. Sheets.Reverse().Select(sheet => sheet.Back)]
        : [.. Sheets.SelectMany(sheet => new[] { sheet.Front, sheet.Back })];
}
