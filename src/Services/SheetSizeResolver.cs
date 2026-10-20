using System;
using PdfBooklet.Configuration;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;

namespace PdfBooklet.Services;

public sealed class SheetSizeResolver : ISheetSizeResolver
{
    public const double PointsPerMillimeter = 72.0 / 25.4;

    public PageSize Resolve(SheetSettings sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var (widthMm, heightMm) = sheet.PaperSize switch
        {
            PaperSize.A3 => (297.0, 420.0),
            PaperSize.A4 => (210.0, 297.0),
            PaperSize.A5 => (148.0, 210.0),
            PaperSize.Letter => (215.9, 279.4),
            PaperSize.Legal => (215.9, 355.6),
            PaperSize.Tabloid => (279.4, 431.8),
            PaperSize.Custom => (sheet.CustomWidthMm, sheet.CustomHeightMm),
            _ => throw new ArgumentOutOfRangeException(nameof(sheet), sheet.PaperSize, "Unknown paper size."),
        };

        var longSide = Math.Max(widthMm, heightMm) * PointsPerMillimeter;
        var shortSide = Math.Min(widthMm, heightMm) * PointsPerMillimeter;
        return new PageSize(longSide, shortSide);
    }
}
