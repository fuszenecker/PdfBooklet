using PdfBooklet.Configuration;
using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class BookletSettingsValidatorTests
{
    private static BookletSettingsValidator CreateValidator() => new(new SheetSizeResolver());

    [TestMethod]
    public void Defaults_AreValid()
    {
        Assert.IsNull(CreateValidator().Validate(new SheetSettings(), new LayoutSettings()));
    }

    [TestMethod]
    public void NegativeMargin_IsRejected()
    {
        var layout = new LayoutSettings { MarginTopMm = -1 };

        Assert.IsNotNull(CreateValidator().Validate(new SheetSettings(), layout));
    }

    [TestMethod]
    public void MarginsConsumingTheSheet_AreRejected()
    {
        var layout = new LayoutSettings { MarginInnerMm = 100, MarginOuterMm = 100 };

        Assert.IsNotNull(CreateValidator().Validate(new SheetSettings(), layout));
    }

    [TestMethod]
    public void SignatureSize_MustBeMultipleOfFour()
    {
        Assert.IsNotNull(CreateValidator().Validate(new SheetSettings(), new LayoutSettings { PagesPerSignature = 6 }));
        Assert.IsNull(CreateValidator().Validate(new SheetSettings(), new LayoutSettings { PagesPerSignature = 8 }));
    }

    [TestMethod]
    public void CustomScale_MustBeInRange()
    {
        var layout = new LayoutSettings { ScaleMode = ScaleMode.Custom, CustomScalePercent = 0 };

        Assert.IsNotNull(CreateValidator().Validate(new SheetSettings(), layout));
    }

    [TestMethod]
    public void CustomScale_IsIgnoredWhenNotInCustomMode()
    {
        var layout = new LayoutSettings { ScaleMode = ScaleMode.FitToArea, CustomScalePercent = 0 };

        Assert.IsNull(CreateValidator().Validate(new SheetSettings(), layout));
    }

    [TestMethod]
    public void CustomSheetSize_MustBeReasonable()
    {
        var sheet = new SheetSettings { PaperSize = PaperSize.Custom, CustomWidthMm = 10, CustomHeightMm = 200 };

        Assert.IsNotNull(CreateValidator().Validate(sheet, new LayoutSettings()));
    }

    [TestMethod]
    public void NaN_IsRejected()
    {
        var layout = new LayoutSettings { MarginOuterMm = double.NaN };

        Assert.IsNotNull(CreateValidator().Validate(new SheetSettings(), layout));
    }
}
