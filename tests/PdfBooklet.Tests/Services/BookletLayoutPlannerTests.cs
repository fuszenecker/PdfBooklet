using PdfBooklet.Configuration;
using PdfBooklet.Models;
using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class BookletLayoutPlannerTests
{
    private static readonly PageSize A5 = new(419.53, 595.28);

    private static BookletLayoutPlanner CreatePlanner() => new(new SheetSizeResolver());

    private static PdfSourceInfo Source(int pageCount) =>
        new("test.pdf", Enumerable.Repeat(A5, pageCount).ToList());

    private static List<int?[]> Order(BookletPlan plan) =>
        plan.Sheets
            .SelectMany(s => new[] { s.Front, s.Back })
            .Select(side => new[] { side.Left.SourcePageIndex, side.Right.SourcePageIndex })
            .ToList();

    [TestMethod]
    public void FourPages_LeftBinding_ImposesClassicBookletOrder()
    {
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings(), new LayoutSettings());

        CollectionAssert.AreEqual(new int?[] { 3, 0 }, Order(plan)[0]);
        CollectionAssert.AreEqual(new int?[] { 1, 2 }, Order(plan)[1]);
        Assert.HasCount(1, plan.Sheets);
    }

    [TestMethod]
    public void EightPages_LeftBinding_NestsSheetsInward()
    {
        var plan = CreatePlanner().Plan(Source(8), new SheetSettings(), new LayoutSettings());

        var order = Order(plan);
        CollectionAssert.AreEqual(new int?[] { 7, 0 }, order[0]);
        CollectionAssert.AreEqual(new int?[] { 1, 6 }, order[1]);
        CollectionAssert.AreEqual(new int?[] { 5, 2 }, order[2]);
        CollectionAssert.AreEqual(new int?[] { 3, 4 }, order[3]);
    }

    [TestMethod]
    public void RightBinding_MirrorsEverySide()
    {
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings(), new LayoutSettings { Binding = BindingEdge.Right });

        CollectionAssert.AreEqual(new int?[] { 0, 3 }, Order(plan)[0]);
        CollectionAssert.AreEqual(new int?[] { 2, 1 }, Order(plan)[1]);
    }

    [TestMethod]
    public void PageCountNotMultipleOfFour_PadsWithBlanksAtTheEnd()
    {
        var plan = CreatePlanner().Plan(Source(5), new SheetSettings(), new LayoutSettings());

        Assert.HasCount(2, plan.Sheets);
        var order = Order(plan);
        CollectionAssert.AreEqual(new int?[] { null, 0 }, order[0]);
        CollectionAssert.AreEqual(new int?[] { 1, null }, order[1]);
        CollectionAssert.AreEqual(new int?[] { null, 2 }, order[2]);
        CollectionAssert.AreEqual(new int?[] { 3, 4 }, order[3]);
    }

    [TestMethod]
    public void Signatures_AreImposedIndependently()
    {
        var plan = CreatePlanner().Plan(Source(10), new SheetSettings(), new LayoutSettings { PagesPerSignature = 4 });

        // 4 + 4 + 2 pages -> 3 signatures of one sheet each, the last padded with 2 blanks.
        Assert.HasCount(3, plan.Sheets);
        var order = Order(plan);
        CollectionAssert.AreEqual(new int?[] { 3, 0 }, order[0]);
        CollectionAssert.AreEqual(new int?[] { 7, 4 }, order[2]);
        CollectionAssert.AreEqual(new int?[] { null, 8 }, order[4]);
        CollectionAssert.AreEqual(new int?[] { 9, null }, order[5]);
    }

    [TestMethod]
    public void Sheet_IsLandscapeRegardlessOfConfiguredOrder()
    {
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings { PaperSize = PaperSize.A4 }, new LayoutSettings());

        Assert.IsGreaterThan(plan.Sheet.Height, plan.Sheet.Width);
        Assert.AreEqual(841.89, plan.Sheet.Width, 0.5);
        Assert.AreEqual(595.28, plan.Sheet.Height, 0.5);
    }

    [TestMethod]
    public void FitToArea_ScalesPageIntoTheMarginBox()
    {
        var layout = new LayoutSettings { MarginTopMm = 0, MarginBottomMm = 0, MarginInnerMm = 0, MarginOuterMm = 0 };
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings(), layout);

        var placed = plan.Sheets[0].Front.Right;
        Assert.AreEqual(1.0, placed.Scale, 0.01);
        Assert.IsGreaterThanOrEqualTo(plan.Sheet.Width / 2, placed.Content.X);
        Assert.IsLessThanOrEqualTo(plan.Sheet.Width, placed.Content.Right + 1e-9);
        Assert.AreEqual(placed.Content.Height, plan.Sheet.Height, 0.5);
    }

    [TestMethod]
    public void ActualSize_KeepsOriginalDimensions()
    {
        var layout = new LayoutSettings { ScaleMode = ScaleMode.ActualSize };
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings(), layout);

        var placed = plan.Sheets[0].Front.Left;
        Assert.AreEqual(1.0, placed.Scale);
        Assert.AreEqual(A5.Width, placed.Content.Width, 1e-9);
        Assert.AreEqual(A5.Height, placed.Content.Height, 1e-9);
    }

    [TestMethod]
    public void CustomScale_AppliesPercentage()
    {
        var layout = new LayoutSettings { ScaleMode = ScaleMode.Custom, CustomScalePercent = 50 };
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings(), layout);

        Assert.AreEqual(0.5, plan.Sheets[0].Front.Left.Scale);
        Assert.AreEqual(A5.Width / 2, plan.Sheets[0].Front.Left.Content.Width, 1e-9);
    }

    [TestMethod]
    public void InnerAlignment_PushesBothPagesTowardsTheFold()
    {
        var layout = new LayoutSettings
        {
            ScaleMode = ScaleMode.Custom,
            CustomScalePercent = 50,
            HorizontalAlignment = PageHorizontalAlignment.Inner,
            MarginInnerMm = 0,
            MarginOuterMm = 0,
        };
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings(), layout);
        var fold = plan.Sheet.Width / 2;

        var side = plan.Sheets[0].Front;
        Assert.AreEqual(fold, side.Left.Content.Right, 1e-6);
        Assert.AreEqual(fold, side.Right.Content.X, 1e-6);
    }

    [TestMethod]
    public void OuterAlignment_PushesBothPagesTowardsTheEdges()
    {
        var layout = new LayoutSettings
        {
            ScaleMode = ScaleMode.Custom,
            CustomScalePercent = 50,
            HorizontalAlignment = PageHorizontalAlignment.Outer,
            MarginInnerMm = 0,
            MarginOuterMm = 0,
        };
        var plan = CreatePlanner().Plan(Source(4), new SheetSettings(), layout);

        var side = plan.Sheets[0].Front;
        Assert.AreEqual(0, side.Left.Content.X, 1e-6);
        Assert.AreEqual(plan.Sheet.Width, side.Right.Content.Right, 1e-6);
    }

    [TestMethod]
    public void VerticalAlignment_PlacesPageAtTopMiddleOrBottom()
    {
        var baseLayout = () => new LayoutSettings { ScaleMode = ScaleMode.Custom, CustomScalePercent = 50, MarginTopMm = 0, MarginBottomMm = 0 };

        var top = CreatePlanner().Plan(Source(4), new SheetSettings(), Apply(baseLayout(), PageVerticalAlignment.Top));
        var middle = CreatePlanner().Plan(Source(4), new SheetSettings(), Apply(baseLayout(), PageVerticalAlignment.Middle));
        var bottom = CreatePlanner().Plan(Source(4), new SheetSettings(), Apply(baseLayout(), PageVerticalAlignment.Bottom));

        var sheetHeight = top.Sheet.Height;
        Assert.AreEqual(0, top.Sheets[0].Front.Left.Content.Y, 1e-6);
        Assert.AreEqual((sheetHeight - A5.Height / 2) / 2, middle.Sheets[0].Front.Left.Content.Y, 1e-6);
        Assert.AreEqual(sheetHeight, bottom.Sheets[0].Front.Left.Content.Bottom, 1e-6);

        static LayoutSettings Apply(LayoutSettings layout, PageVerticalAlignment alignment)
        {
            layout.VerticalAlignment = alignment;
            return layout;
        }
    }

    [TestMethod]
    public void EmptyDocument_ProducesNoSheets()
    {
        var plan = CreatePlanner().Plan(new PdfSourceInfo("empty.pdf", []), new SheetSettings(), new LayoutSettings());

        Assert.IsEmpty(plan.Sheets);
    }
}
