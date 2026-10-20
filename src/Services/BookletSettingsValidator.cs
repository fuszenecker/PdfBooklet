using System;
using PdfBooklet.Configuration;
using PdfBooklet.Interfaces;

namespace PdfBooklet.Services;

public sealed class BookletSettingsValidator : IBookletSettingsValidator
{
    private const double MinSheetMm = 50;
    private const double MaxSheetMm = 2000;
    private const double MinAreaPoints = 20;

    private readonly ISheetSizeResolver _sheetSizeResolver;

    public BookletSettingsValidator(ISheetSizeResolver sheetSizeResolver)
    {
        _sheetSizeResolver = sheetSizeResolver;
    }

    public string? Validate(SheetSettings sheet, LayoutSettings layout)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(layout);

        if (sheet.PaperSize == PaperSize.Custom
            && !(IsInRange(sheet.CustomWidthMm, MinSheetMm, MaxSheetMm) && IsInRange(sheet.CustomHeightMm, MinSheetMm, MaxSheetMm)))
        {
            return $"The custom sheet size must be between {MinSheetMm:0} and {MaxSheetMm:0} mm in both directions.";
        }

        if (!(IsFinite(layout.MarginTopMm) && layout.MarginTopMm >= 0
            && IsFinite(layout.MarginBottomMm) && layout.MarginBottomMm >= 0
            && IsFinite(layout.MarginInnerMm) && layout.MarginInnerMm >= 0
            && IsFinite(layout.MarginOuterMm) && layout.MarginOuterMm >= 0))
        {
            return "Margins cannot be negative.";
        }

        if (layout.ScaleMode == ScaleMode.Custom && !IsInRange(layout.CustomScalePercent, 1, 1000))
        {
            return "The custom scale must be between 1 % and 1000 %.";
        }

        if (layout.PagesPerSignature < 0 || layout.PagesPerSignature % 4 != 0)
        {
            return "Pages per signature must be zero (one signature) or a multiple of 4.";
        }

        var size = _sheetSizeResolver.Resolve(sheet);
        var areaWidth = size.Width / 2 - (layout.MarginInnerMm + layout.MarginOuterMm) * SheetSizeResolver.PointsPerMillimeter;
        var areaHeight = size.Height - (layout.MarginTopMm + layout.MarginBottomMm) * SheetSizeResolver.PointsPerMillimeter;
        if (areaWidth < MinAreaPoints || areaHeight < MinAreaPoints)
        {
            return "The margins leave no room for the pages on the sheet.";
        }

        return null;
    }

    private static bool IsFinite(double value) => double.IsFinite(value);

    private static bool IsInRange(double value, double min, double max) => double.IsFinite(value) && value >= min && value <= max;
}
