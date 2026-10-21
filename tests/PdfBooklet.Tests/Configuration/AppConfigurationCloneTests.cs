using PdfBooklet.Configuration;

namespace PdfBooklet.Tests.Configuration;

[TestClass]
public sealed class AppConfigurationCloneTests
{
    [TestMethod]
    public void Clone_IsDeepAndIndependent()
    {
        var original = new AppConfiguration();
        original.Sheet.PaperSize = PaperSize.A3;
        original.Layout.PagesPerSignature = 16;
        original.Output.PdfVersion = PdfVersion.Pdf14;
        original.UiPreferences.LastSourceDirectory = "/a";

        var clone = original.Clone();
        original.Sheet.PaperSize = PaperSize.A5;
        original.Layout.PagesPerSignature = 4;
        original.Output.PdfVersion = PdfVersion.Pdf17;
        original.UiPreferences.LastSourceDirectory = "/b";

        Assert.AreEqual(PaperSize.A3, clone.Sheet.PaperSize);
        Assert.AreEqual(16, clone.Layout.PagesPerSignature);
        Assert.AreEqual(PdfVersion.Pdf14, clone.Output.PdfVersion);
        Assert.AreEqual("/a", clone.UiPreferences.LastSourceDirectory);
    }

    [TestMethod]
    public void Clone_CopiesEveryLayoutProperty()
    {
        var layout = new LayoutSettings
        {
            Binding = BindingEdge.Right,
            MarginTopMm = 1,
            MarginBottomMm = 2,
            MarginInnerMm = 3,
            MarginOuterMm = 4,
            ScaleMode = ScaleMode.Custom,
            CustomScalePercent = 55,
            HorizontalAlignment = PageHorizontalAlignment.Outer,
            VerticalAlignment = PageVerticalAlignment.Bottom,
            PagesPerSignature = 12,
        };

        var clone = layout.Clone();

        foreach (var property in typeof(LayoutSettings).GetProperties())
        {
            Assert.AreEqual(property.GetValue(layout), property.GetValue(clone), property.Name);
        }
    }

    [TestMethod]
    public void Clone_CopiesEveryOutputProperty()
    {
        var output = new OutputSettings { PdfVersion = PdfVersion.Pdf14, CompressStreams = false, ManualDuplex = true };

        var clone = output.Clone();

        foreach (var property in typeof(OutputSettings).GetProperties())
        {
            Assert.AreEqual(property.GetValue(output), property.GetValue(clone), property.Name);
        }
    }

    [TestMethod]
    public void Clone_ToleratesNullSections()
    {
        var config = new AppConfiguration { Sheet = null!, Layout = null!, Output = null!, UiPreferences = null! };

        var clone = config.Clone();

        Assert.IsNotNull(clone.Sheet);
        Assert.IsNotNull(clone.Layout);
        Assert.IsNotNull(clone.Output);
        Assert.IsNotNull(clone.UiPreferences);
    }
}
