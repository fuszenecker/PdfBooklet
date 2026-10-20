using PdfBooklet.Configuration;
using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class SheetSizeResolverTests
{
    private static double ToMm(double points) => points / SheetSizeResolver.PointsPerMillimeter;

    [TestMethod]
    [DataRow(PaperSize.A3, 420, 297)]
    [DataRow(PaperSize.A4, 297, 210)]
    [DataRow(PaperSize.A5, 210, 148)]
    [DataRow(PaperSize.Letter, 279.4, 215.9)]
    [DataRow(PaperSize.Legal, 355.6, 215.9)]
    [DataRow(PaperSize.Tabloid, 431.8, 279.4)]
    public void StandardSizes_AreLandscapeInMillimeters(PaperSize paper, double widthMm, double heightMm)
    {
        var size = new SheetSizeResolver().Resolve(new SheetSettings { PaperSize = paper });

        Assert.AreEqual(widthMm, ToMm(size.Width), 0.01);
        Assert.AreEqual(heightMm, ToMm(size.Height), 0.01);
    }

    [TestMethod]
    public void CustomSize_IsNormalizedToLandscape()
    {
        var portrait = new SheetSettings { PaperSize = PaperSize.Custom, CustomWidthMm = 200, CustomHeightMm = 300 };

        var size = new SheetSizeResolver().Resolve(portrait);

        Assert.AreEqual(300, ToMm(size.Width), 0.01);
        Assert.AreEqual(200, ToMm(size.Height), 0.01);
    }

    [TestMethod]
    public void UnknownPaperSize_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new SheetSizeResolver().Resolve(new SheetSettings { PaperSize = (PaperSize)999 }));
    }
}
