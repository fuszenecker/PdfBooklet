using System.Globalization;
using PdfBooklet.Configuration;
using PdfBooklet.Converters;

namespace PdfBooklet.Tests.Converters;

[TestClass]
public sealed class EnumDisplayConverterTests
{
    private static string Convert(object? value) =>
        (string)new EnumDisplayConverter().Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

    [TestMethod]
    public void PascalCase_BecomesSentenceCase()
    {
        Assert.AreEqual("Fit to area", Convert(ScaleMode.FitToArea));
        Assert.AreEqual("Actual size", Convert(ScaleMode.ActualSize));
    }

    [TestMethod]
    public void SingleWord_IsUnchanged()
    {
        Assert.AreEqual("Left", Convert(BindingEdge.Left));
        Assert.AreEqual("A4", Convert(PaperSize.A4));
    }

    [TestMethod]
    public void PdfVersions_AreShownWithDot()
    {
        Assert.AreEqual("PDF 1.4", Convert(PdfVersion.Pdf14));
        Assert.AreEqual("PDF 1.7", Convert(PdfVersion.Pdf17));
    }

    [TestMethod]
    public void Null_IsEmpty()
    {
        Assert.AreEqual(string.Empty, Convert(null));
    }

    [TestMethod]
    public void ConvertBack_IsNotSupported()
    {
        Assert.ThrowsExactly<NotSupportedException>(
            () => new EnumDisplayConverter().ConvertBack("x", typeof(ScaleMode), null, CultureInfo.InvariantCulture));
    }
}
