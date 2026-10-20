using System;
using System.Collections.Generic;
using PdfBooklet.Configuration;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;

namespace PdfBooklet.Services;

public sealed class BookletLayoutPlanner : IBookletLayoutPlanner
{
    private readonly ISheetSizeResolver _sheetSizeResolver;

    public BookletLayoutPlanner(ISheetSizeResolver sheetSizeResolver)
    {
        _sheetSizeResolver = sheetSizeResolver;
    }

    public BookletPlan Plan(PdfSourceInfo source, SheetSettings sheet, LayoutSettings layout)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(layout);

        var sheetSize = _sheetSizeResolver.Resolve(sheet);
        var geometry = new Geometry(sheetSize, layout, source.Pages.Count > 0 ? source.Pages[0] : default);

        var sheets = new List<BookletSheet>();
        foreach (var signature in SplitIntoSignatures(source.PageCount, layout.PagesPerSignature))
        {
            AppendSignature(sheets, signature.Offset, signature.RealPages, signature.Slots, layout.Binding, source, geometry);
        }

        return new BookletPlan(sheetSize, sheets);
    }

    private static IEnumerable<(int Offset, int RealPages, int Slots)> SplitIntoSignatures(int pageCount, int pagesPerSignature)
    {
        if (pageCount == 0)
        {
            yield break;
        }

        var chunk = pagesPerSignature > 0 ? pagesPerSignature : pageCount;
        for (var offset = 0; offset < pageCount; offset += chunk)
        {
            var real = Math.Min(chunk, pageCount - offset);
            var slots = (real + 3) / 4 * 4;
            yield return (offset, real, slots);
        }
    }

    private static void AppendSignature(
        List<BookletSheet> sheets,
        int offset,
        int realPages,
        int slots,
        BindingEdge binding,
        PdfSourceInfo source,
        Geometry geometry)
    {
        PlacedPage Place(int slot, bool isLeft)
        {
            int? sourceIndex = slot < realPages ? offset + slot : null;
            return geometry.Place(sourceIndex, sourceIndex is { } index ? source.Pages[index] : null, isLeft);
        }

        for (var i = 0; i < slots / 2; i += 2)
        {
            // Sheet i/2: the outermost remaining pair of the signature on the front, the
            // next pair inwards on the back. Right-to-left binding mirrors every side.
            var frontLeft = slots - 1 - i;
            var frontRight = i;
            var backLeft = i + 1;
            var backRight = slots - 2 - i;

            if (binding == BindingEdge.Right)
            {
                (frontLeft, frontRight) = (frontRight, frontLeft);
                (backLeft, backRight) = (backRight, backLeft);
            }

            sheets.Add(new BookletSheet(
                new SheetSide(Place(frontLeft, isLeft: true), Place(frontRight, isLeft: false)),
                new SheetSide(Place(backLeft, isLeft: true), Place(backRight, isLeft: false))));
        }
    }

    private sealed class Geometry
    {
        private readonly PageSize _sheet;
        private readonly LayoutSettings _layout;
        private readonly PageSize _blankSize;

        private readonly double _top;
        private readonly double _areaHeight;
        private readonly double _inner;
        private readonly double _outer;

        public Geometry(PageSize sheet, LayoutSettings layout, PageSize firstPage)
        {
            _sheet = sheet;
            _layout = layout;
            _blankSize = firstPage.Width > 0 && firstPage.Height > 0
                ? firstPage
                : new PageSize(sheet.Width / 2, sheet.Height);

            const double mm = SheetSizeResolver.PointsPerMillimeter;
            _top = layout.MarginTopMm * mm;
            _areaHeight = sheet.Height - (layout.MarginTopMm + layout.MarginBottomMm) * mm;
            _inner = layout.MarginInnerMm * mm;
            _outer = layout.MarginOuterMm * mm;
        }

        public PlacedPage Place(int? sourceIndex, PageSize? pageSize, bool isLeft)
        {
            var half = isLeft
                ? new PageRect(0, 0, _sheet.Width / 2, _sheet.Height)
                : new PageRect(_sheet.Width / 2, 0, _sheet.Width / 2, _sheet.Height);

            var areaX = isLeft ? _outer : half.X + _inner;
            var areaWidth = half.Width - _inner - _outer;
            var page = pageSize ?? _blankSize;

            var scale = _layout.ScaleMode switch
            {
                ScaleMode.FitToArea => Math.Min(areaWidth / page.Width, _areaHeight / page.Height),
                ScaleMode.ActualSize => 1.0,
                ScaleMode.Custom => _layout.CustomScalePercent / 100.0,
                _ => throw new InvalidOperationException($"Unknown scale mode {_layout.ScaleMode}."),
            };

            var width = page.Width * scale;
            var height = page.Height * scale;

            // Inner/outer are relative to the spine, so they flip between the two halves.
            var towardsSpineIsRight = isLeft;
            var x = _layout.HorizontalAlignment switch
            {
                PageHorizontalAlignment.Center => areaX + (areaWidth - width) / 2,
                PageHorizontalAlignment.Inner => towardsSpineIsRight ? areaX + areaWidth - width : areaX,
                PageHorizontalAlignment.Outer => towardsSpineIsRight ? areaX : areaX + areaWidth - width,
                _ => throw new InvalidOperationException($"Unknown alignment {_layout.HorizontalAlignment}."),
            };

            var y = _layout.VerticalAlignment switch
            {
                PageVerticalAlignment.Top => _top,
                PageVerticalAlignment.Middle => _top + (_areaHeight - height) / 2,
                PageVerticalAlignment.Bottom => _top + _areaHeight - height,
                _ => throw new InvalidOperationException($"Unknown alignment {_layout.VerticalAlignment}."),
            };

            return new PlacedPage(sourceIndex, half, new PageRect(x, y, width, height), scale);
        }
    }
}
