using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PdfBooklet.Models;

namespace PdfBooklet.Views.Controls;

/// <summary>
/// Draws the front and back of one planned sheet: the paper, the fold, and where every source
/// page lands. It renders <see cref="BookletPlan"/> directly, so what it shows is exactly
/// what the exporter will place.
/// </summary>
public sealed class BookletPreview : Control
{
    public static readonly StyledProperty<BookletPlan?> PlanProperty =
        AvaloniaProperty.Register<BookletPreview, BookletPlan?>(nameof(Plan));

    public static readonly StyledProperty<int> SheetIndexProperty =
        AvaloniaProperty.Register<BookletPreview, int>(nameof(SheetIndex));

    private const double Gap = 28;
    private const double Padding = 12;

    private static readonly IBrush PaperBrush = Brushes.White;
    private static readonly IPen PaperPen = new Pen(new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A)), 1);
    private static readonly IPen FoldPen = new Pen(new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)), 1, new DashStyle([4, 4], 0));
    private static readonly IBrush PageBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0xE4, 0xFA));
    private static readonly IPen PagePen = new Pen(new SolidColorBrush(Color.FromRgb(0x3B, 0x6E, 0xC4)), 1);
    private static readonly IBrush BlankBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
    private static readonly IPen BlankPen = new Pen(new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xB8)), 1, new DashStyle([3, 3], 0));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x3C, 0x73));

    static BookletPreview()
    {
        AffectsRender<BookletPreview>(PlanProperty, SheetIndexProperty);
    }

    public BookletPlan? Plan
    {
        get => GetValue(PlanProperty);
        set => SetValue(PlanProperty, value);
    }

    public int SheetIndex
    {
        get => GetValue(SheetIndexProperty);
        set => SetValue(SheetIndexProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Plan is not { } plan || SheetIndex < 0 || SheetIndex >= plan.Sheets.Count)
        {
            return;
        }

        var sheet = plan.Sheets[SheetIndex];
        var labelHeight = 20.0;
        var availableWidth = Math.Max(1, Bounds.Width - 2 * Padding);
        var availableHeight = Math.Max(1, Bounds.Height - 2 * Padding - Gap - 2 * labelHeight);
        var scale = Math.Min(availableWidth / plan.Sheet.Width, availableHeight / (2 * plan.Sheet.Height));
        if (scale <= 0)
        {
            return;
        }

        var drawnWidth = plan.Sheet.Width * scale;
        var drawnHeight = plan.Sheet.Height * scale;
        var left = (Bounds.Width - drawnWidth) / 2;

        DrawSide(context, "Front", sheet.Front, new Point(left, Padding + labelHeight), scale, drawnWidth, drawnHeight);
        DrawSide(context, "Back", sheet.Back, new Point(left, Padding + 2 * labelHeight + drawnHeight + Gap), scale, drawnWidth, drawnHeight);
    }

    private void DrawSide(DrawingContext context, string label, SheetSide side, Point origin, double scale, double width, double height)
    {
        DrawText(context, label, FontWeight.SemiBold, 12, Brushes.White, new Point(origin.X, origin.Y - 18));

        context.DrawRectangle(PaperBrush, PaperPen, new Rect(origin, new Size(width, height)));
        context.DrawLine(FoldPen, new Point(origin.X + width / 2, origin.Y), new Point(origin.X + width / 2, origin.Y + height));

        DrawPage(context, side.Left, origin, scale);
        DrawPage(context, side.Right, origin, scale);
    }

    private static void DrawPage(DrawingContext context, PlacedPage page, Point origin, double scale)
    {
        var rect = new Rect(
            origin.X + page.Content.X * scale,
            origin.Y + page.Content.Y * scale,
            page.Content.Width * scale,
            page.Content.Height * scale);

        var isBlank = page.SourcePageIndex is null;
        using (context.PushClip(new Rect(
                   origin.X + page.HalfSheet.X * scale,
                   origin.Y + page.HalfSheet.Y * scale,
                   page.HalfSheet.Width * scale,
                   page.HalfSheet.Height * scale)))
        {
            context.DrawRectangle(isBlank ? BlankBrush : PageBrush, isBlank ? BlankPen : PagePen, rect);

            var text = isBlank ? "blank" : (page.SourcePageIndex!.Value + 1).ToString(CultureInfo.InvariantCulture);
            var size = Math.Clamp(Math.Min(rect.Width, rect.Height) / 3, 9, 28);
            var formatted = CreateText(text, FontWeight.SemiBold, size, TextBrush);
            context.DrawText(formatted, new Point(rect.X + (rect.Width - formatted.Width) / 2, rect.Y + (rect.Height - formatted.Height) / 2));
        }
    }

    private static void DrawText(DrawingContext context, string text, FontWeight weight, double size, IBrush brush, Point at) =>
        context.DrawText(CreateText(text, weight, size, brush), at);

    private static FormattedText CreateText(string text, FontWeight weight, double size, IBrush brush) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default, FontStyle.Normal, weight), size, brush);
}
