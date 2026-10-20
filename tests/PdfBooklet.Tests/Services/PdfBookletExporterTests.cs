using PdfBooklet.Configuration;
using PdfBooklet.Exceptions;
using PdfBooklet.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class PdfBookletExporterTests
{
    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), "pdfbooklet-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void DeleteDirectory()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string CreateSourcePdf(int pageCount, int rotate = 0)
    {
        var path = Path.Combine(_directory, "source.pdf");
        using var document = new PdfDocument();
        for (var i = 0; i < pageCount; i++)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromMillimeter(148);
            page.Height = XUnit.FromMillimeter(210);
            page.Rotate = rotate;
            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(XBrushes.SteelBlue, new XRect(20, 20, 100, 100));
        }

        document.Save(path);
        return path;
    }

    [TestMethod]
    public async Task Reader_ReportsPageCountAndSizes()
    {
        var path = CreateSourcePdf(6);

        var info = await new PdfSourceReader().ReadAsync(path);

        Assert.AreEqual(6, info.PageCount);
        Assert.AreEqual(148 * SheetSizeResolver.PointsPerMillimeter, info.Pages[0].Width, 0.5);
        Assert.AreEqual(210 * SheetSizeResolver.PointsPerMillimeter, info.Pages[0].Height, 0.5);
    }

    [TestMethod]
    public async Task Reader_SwapsDimensionsOfRotatedPages()
    {
        var path = CreateSourcePdf(1, rotate: 90);

        var info = await new PdfSourceReader().ReadAsync(path);

        Assert.IsGreaterThan(info.Pages[0].Height, info.Pages[0].Width);
    }

    [TestMethod]
    public async Task Reader_RejectsFilesThatAreNotPdf()
    {
        var path = Path.Combine(_directory, "not-a.pdf");
        await File.WriteAllTextAsync(path, "this is not a pdf");

        await Assert.ThrowsExactlyAsync<PdfBookletException>(() => new PdfSourceReader().ReadAsync(path));
    }

    [TestMethod]
    public async Task Export_WritesTwoSidesPerSheetOnTheConfiguredPaper()
    {
        var sourcePath = CreateSourcePdf(6);
        var source = await new PdfSourceReader().ReadAsync(sourcePath);
        var sheetSettings = new SheetSettings { PaperSize = PaperSize.A4 };
        var plan = new BookletLayoutPlanner(new SheetSizeResolver()).Plan(source, sheetSettings, new LayoutSettings());
        var destination = Path.Combine(_directory, "booklet.pdf");

        await new PdfBookletExporter().ExportAsync(source, plan, new OutputSettings { PdfVersion = PdfVersion.Pdf14 }, destination);

        using var result = PdfReader.Open(destination, PdfDocumentOpenMode.Import);
        Assert.AreEqual(4, result.PageCount);
        Assert.AreEqual(14, result.Version);
        Assert.AreEqual(297, result.Pages[0].Width.Millimeter, 0.5);
        Assert.AreEqual(210, result.Pages[0].Height.Millimeter, 0.5);
    }

    [TestMethod]
    public async Task Export_LeavesNoTemporaryFilesBehind()
    {
        var source = await new PdfSourceReader().ReadAsync(CreateSourcePdf(4));
        var plan = new BookletLayoutPlanner(new SheetSizeResolver()).Plan(source, new SheetSettings(), new LayoutSettings());
        var destination = Path.Combine(_directory, "booklet.pdf");

        await new PdfBookletExporter().ExportAsync(source, plan, new OutputSettings(), destination);

        CollectionAssert.AreEquivalent(
            new[] { "booklet.pdf", "source.pdf" },
            Directory.EnumerateFileSystemEntries(_directory).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public async Task Export_ToMissingDirectory_ThrowsPdfBookletException()
    {
        var source = await new PdfSourceReader().ReadAsync(CreateSourcePdf(4));
        var plan = new BookletLayoutPlanner(new SheetSizeResolver()).Plan(source, new SheetSettings(), new LayoutSettings());
        var destination = Path.Combine(_directory, "missing", "booklet.pdf");

        await Assert.ThrowsExactlyAsync<PdfBookletException>(
            () => new PdfBookletExporter().ExportAsync(source, plan, new OutputSettings(), destination));
    }
}
