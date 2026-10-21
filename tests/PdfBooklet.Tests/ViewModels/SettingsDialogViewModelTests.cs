using PdfBooklet.Configuration;
using PdfBooklet.Models;
using PdfBooklet.Services;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Tests.ViewModels;

[TestClass]
public sealed class SettingsDialogViewModelTests
{
    private static SettingsDialogViewModel Create(BookletSettings? settings = null) =>
        new(settings ?? new BookletSettings(new SheetSettings(), new LayoutSettings(), new OutputSettings()),
            new BookletSettingsValidator(new SheetSizeResolver()));

    [TestMethod]
    public void Build_RoundTripsTheInitialSettings()
    {
        var initial = new BookletSettings(
            new SheetSettings { PaperSize = PaperSize.Letter },
            new LayoutSettings { Binding = BindingEdge.Right, MarginInnerMm = 7, PagesPerSignature = 16 },
            new OutputSettings { PdfVersion = PdfVersion.Pdf15, CompressStreams = false, ManualDuplex = true });

        var built = Create(initial).Build();

        Assert.AreEqual(PaperSize.Letter, built.Sheet.PaperSize);
        Assert.AreEqual(BindingEdge.Right, built.Layout.Binding);
        Assert.AreEqual(7, built.Layout.MarginInnerMm);
        Assert.AreEqual(16, built.Layout.PagesPerSignature);
        Assert.AreEqual(PdfVersion.Pdf15, built.Output.PdfVersion);
        Assert.IsFalse(built.Output.CompressStreams);
        Assert.IsTrue(built.Output.ManualDuplex);
    }

    [TestMethod]
    public void InvalidValues_DisableOkAndShowMessage()
    {
        var viewModel = Create();
        Assert.IsTrue(viewModel.OkCommand.CanExecute(null));

        viewModel.PagesPerSignature = 6;

        Assert.IsTrue(viewModel.HasValidationMessage);
        Assert.IsFalse(viewModel.OkCommand.CanExecute(null));

        viewModel.PagesPerSignature = 8;

        Assert.IsFalse(viewModel.HasValidationMessage);
        Assert.IsTrue(viewModel.OkCommand.CanExecute(null));
    }

    [TestMethod]
    public void OkAndCancel_RaiseCloseRequestedWithOutcome()
    {
        var viewModel = Create();
        var outcomes = new List<bool>();
        viewModel.CloseRequested += (_, accepted) => outcomes.Add(accepted);

        viewModel.OkCommand.Execute(null);
        viewModel.CancelCommand.Execute(null);

        CollectionAssert.AreEqual(new[] { true, false }, outcomes);
    }

    [TestMethod]
    public void CustomPaperSize_TogglesDimensionFields()
    {
        var viewModel = Create();
        Assert.IsFalse(viewModel.IsCustomPaperSize);

        viewModel.PaperSize = PaperSize.Custom;

        Assert.IsTrue(viewModel.IsCustomPaperSize);
    }
}
