using NSubstitute;
using PdfBooklet.Configuration;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;
using PdfBooklet.Services;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Tests.ViewModels;

[TestClass]
public sealed class SettingsDialogViewModelTests
{
    private static readonly PdfViewer Okular = new("Okular", "/usr/bin/okular");
    private static readonly PdfViewer Evince = new("Evince", "/usr/bin/evince");

    private Func<string?, Task<string?>> _pickApplication = _ => Task.FromResult<string?>(null);

    private SettingsDialogViewModel Create(BookletSettings? settings = null, params PdfViewer[] installed)
    {
        var catalog = Substitute.For<IPdfViewerCatalog>();
        catalog.InstalledViewers.Returns(installed);
        return new(settings ?? new BookletSettings(new SheetSettings(), new LayoutSettings(), new OutputSettings()),
            new BookletSettingsValidator(new SheetSizeResolver()),
            catalog,
            directory => _pickApplication(directory));
    }

    private static BookletSettings WithViewer(string? viewer, bool open = true) =>
        new(new SheetSettings(), new LayoutSettings(), new OutputSettings { OpenAfterExport = open, Viewer = viewer });

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

    [TestMethod]
    public void Viewers_SystemDefaultFirstThenInstalledThenCustomLast()
    {
        var viewModel = Create(null, Okular, Evince);

        CollectionAssert.AreEqual(
            new[] { "System default", "Okular", "Evince", "Custom…" },
            viewModel.Viewers.Select(v => v.DisplayName).ToArray());
        Assert.AreSame(PdfViewerOption.SystemDefault, viewModel.SelectedViewer);
        Assert.IsTrue(viewModel.OpenAfterExport);
    }

    [TestMethod]
    public void SavedInstalledViewer_IsSelected()
    {
        var viewModel = Create(WithViewer("/usr/bin/evince", open: false), Okular, Evince);

        Assert.AreEqual("Evince", viewModel.SelectedViewer.DisplayName);
        Assert.IsFalse(viewModel.OpenAfterExport);
        Assert.HasCount(4, viewModel.Viewers);
    }

    [TestMethod]
    public void SavedUnknownViewer_IsShownAsCustomBeforeTheLastEntry()
    {
        var viewModel = Create(WithViewer("/opt/viewer/bin/mypdf"), Okular);

        Assert.AreEqual(PdfViewerOptionKind.Custom, viewModel.SelectedViewer.Kind);
        Assert.AreEqual("mypdf (custom)", viewModel.SelectedViewer.DisplayName);
        CollectionAssert.AreEqual(
            new[] { PdfViewerOptionKind.SystemDefault, PdfViewerOptionKind.Installed, PdfViewerOptionKind.Custom, PdfViewerOptionKind.Browse },
            viewModel.Viewers.Select(v => v.Kind).ToArray());
    }

    [TestMethod]
    public void ChoosingCustom_PicksAProgramAndSelectsIt()
    {
        var viewModel = Create(WithViewer("/opt/old/viewer"), Okular);
        string? startDirectory = "unset";
        _pickApplication = directory =>
        {
            startDirectory = directory;
            return Task.FromResult<string?>("/opt/new/viewer2");
        };

        viewModel.SelectedViewer = PdfViewerOption.Browse;

        Assert.AreEqual("/opt/old", startDirectory);
        Assert.AreEqual("/opt/new/viewer2", viewModel.SelectedViewer.Path);
        // The new program replaces the previous custom entry.
        Assert.AreEqual(1, viewModel.Viewers.Count(v => v.Kind == PdfViewerOptionKind.Custom));
        Assert.AreSame(PdfViewerOption.Browse, viewModel.Viewers[^1]);
        Assert.AreEqual("/opt/new/viewer2", viewModel.Build().Output.Viewer);
    }

    [TestMethod]
    public void ChoosingCustom_AnInstalledProgramSelectsItsEntry()
    {
        var viewModel = Create(null, Okular);
        _pickApplication = _ => Task.FromResult<string?>("/usr/bin/okular");

        viewModel.SelectedViewer = PdfViewerOption.Browse;

        Assert.AreEqual("Okular", viewModel.SelectedViewer.DisplayName);
        Assert.HasCount(3, viewModel.Viewers);
    }

    [TestMethod]
    public void ChoosingCustom_CancelledGoesBackToThePreviousChoice()
    {
        var viewModel = Create(null, Okular, Evince);
        viewModel.SelectedViewer = viewModel.Viewers[2];

        viewModel.SelectedViewer = PdfViewerOption.Browse;

        Assert.AreEqual("Evince", viewModel.SelectedViewer.DisplayName);
        Assert.AreEqual("/usr/bin/evince", viewModel.Build().Output.Viewer);
    }

    [TestMethod]
    public void SystemDefault_BuildsANullViewer()
    {
        var viewModel = Create(WithViewer("/usr/bin/okular"), Okular);

        viewModel.SelectedViewer = PdfViewerOption.SystemDefault;
        viewModel.OpenAfterExport = false;
        var output = viewModel.Build().Output;

        Assert.IsNull(output.Viewer);
        Assert.IsFalse(output.OpenAfterExport);
    }
}
