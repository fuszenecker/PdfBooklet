using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfBooklet.Configuration;
using PdfBooklet.Exceptions;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;
using PdfBooklet.Presentation;
using PdfBooklet.Services;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Tests.ViewModels;

[TestClass]
public sealed class MainWindowViewModelTests
{
    private static readonly PageSize A5 = new(419.53, 595.28);

    private IPdfFileDialogService _files = null!;
    private ISettingsDialogService _settingsDialog = null!;
    private IPdfSourceReader _reader = null!;
    private IPdfBookletExporter _exporter = null!;
    private IConfigurationService _configuration = null!;
    private IInfoDialogService _info = null!;
    private AppConfiguration _config = null!;

    private MainWindowViewModel CreateViewModel()
    {
        _files = Substitute.For<IPdfFileDialogService>();
        _settingsDialog = Substitute.For<ISettingsDialogService>();
        _reader = Substitute.For<IPdfSourceReader>();
        _exporter = Substitute.For<IPdfBookletExporter>();
        _info = Substitute.For<IInfoDialogService>();
        _configuration = Substitute.For<IConfigurationService>();
        _config = new AppConfiguration();
        _configuration.Current.Returns(_ => _config);

        var resolver = new SheetSizeResolver();
        return new MainWindowViewModel(
            _files,
            _settingsDialog,
            _reader,
            new BookletLayoutPlanner(resolver),
            new BookletSettingsValidator(resolver),
            _exporter,
            _configuration,
            _info,
            NullLogger<MainWindowViewModel>.Instance);
    }

    private static PdfSourceInfo Source(string path, int pages) =>
        new(path, Enumerable.Repeat(A5, pages).ToList());

    private async Task<MainWindowViewModel> LoadedAsync(int pages = 6, string path = "/books/novel.pdf")
    {
        var viewModel = CreateViewModel();
        _reader.ReadAsync(path, Arg.Any<CancellationToken>()).Returns(Source(path, pages));
        await viewModel.LoadSourceAsync(path);
        return viewModel;
    }

    [TestMethod]
    public void Initially_NothingToExportOrPreview()
    {
        var viewModel = CreateViewModel();

        Assert.IsNull(viewModel.Plan);
        Assert.IsFalse(viewModel.HasSource);
        Assert.IsFalse(viewModel.ExportCommand.CanExecute(null));
        Assert.IsTrue(viewModel.OpenPdfCommand.CanExecute(null));
        Assert.IsTrue(viewModel.OpenSettingsCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task LoadSource_BuildsPlanAndEnablesExport()
    {
        var viewModel = await LoadedAsync(pages: 6);

        Assert.IsTrue(viewModel.HasSource);
        Assert.IsNotNull(viewModel.Plan);
        Assert.HasCount(2, viewModel.Plan.Sheets);
        Assert.IsTrue(viewModel.ExportCommand.CanExecute(null));
        Assert.Contains("novel.pdf", viewModel.SourceSummary);
        Assert.Contains("2 blank pages", viewModel.LayoutSummary);
        Assert.AreEqual("Sheet 1 / 2", viewModel.SheetLabel);
    }

    [TestMethod]
    public async Task LoadSource_RemembersTheDirectory()
    {
        await LoadedAsync(path: "/books/novel.pdf");

        _configuration.Received().Apply(Arg.Is<AppConfiguration>(c => c.UiPreferences.LastSourceDirectory == "/books"));
    }

    [TestMethod]
    public async Task LoadSource_Failure_ShowsDialogAndKeepsNoPlan()
    {
        var viewModel = CreateViewModel();
        _reader.ReadAsync("/bad.pdf", Arg.Any<CancellationToken>())
            .Returns<PdfSourceInfo>(_ => throw new PdfBookletException("not a pdf"));

        await viewModel.LoadSourceAsync("/bad.pdf");

        await _info.Received(1).ShowInfoAsync("Open PDF", "not a pdf");
        Assert.IsNull(viewModel.Plan);
        Assert.IsFalse(viewModel.HasSource);
        Assert.IsFalse(viewModel.IsBusy);
    }

    [TestMethod]
    public async Task OpenPdf_WhenPickerCancelled_DoesNothing()
    {
        var viewModel = CreateViewModel();
        _files.PickSourceAsync(Arg.Any<string?>()).Returns((string?)null);

        await viewModel.OpenPdfCommand.ExecuteAsync(null);

        await _reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default);
        Assert.IsFalse(viewModel.IsBusy);
    }

    [TestMethod]
    public async Task OpenPdf_PassesTheLastDirectoryToThePicker()
    {
        var viewModel = CreateViewModel();
        _config.UiPreferences.LastSourceDirectory = "/last";
        _files.PickSourceAsync("/last").Returns((string?)null);

        await viewModel.OpenPdfCommand.ExecuteAsync(null);

        await _files.Received(1).PickSourceAsync("/last");
    }

    [TestMethod]
    public async Task Settings_Accepted_AppliesPersistsAndReplans()
    {
        var viewModel = await LoadedAsync();
        var before = viewModel.Plan!.Sheet;
        var changed = new BookletSettings(
            new SheetSettings { PaperSize = PaperSize.A3 }, new LayoutSettings(), new OutputSettings { PdfVersion = PdfVersion.Pdf14 });
        _settingsDialog.ShowAsync(Arg.Any<BookletSettings>()).Returns(changed);

        await viewModel.OpenSettingsCommand.ExecuteAsync(null);

        Assert.IsGreaterThan(before.Width, viewModel.Plan!.Sheet.Width);
        _configuration.Received().Apply(Arg.Is<AppConfiguration>(c =>
            c.Sheet.PaperSize == PaperSize.A3 && c.Output.PdfVersion == PdfVersion.Pdf14));
    }

    [TestMethod]
    public async Task Settings_Cancelled_ChangesNothing()
    {
        var viewModel = await LoadedAsync();
        _configuration.ClearReceivedCalls();
        var plan = viewModel.Plan;
        _settingsDialog.ShowAsync(Arg.Any<BookletSettings>()).Returns((BookletSettings?)null);

        await viewModel.OpenSettingsCommand.ExecuteAsync(null);

        Assert.AreSame(plan, viewModel.Plan);
        _configuration.DidNotReceiveWithAnyArgs().Apply(default!);
    }

    [TestMethod]
    public async Task Settings_DialogIsGivenTheCurrentSettings()
    {
        var viewModel = CreateViewModel();
        _settingsDialog.ShowAsync(Arg.Any<BookletSettings>()).Returns((BookletSettings?)null);

        await viewModel.OpenSettingsCommand.ExecuteAsync(null);

        await _settingsDialog.Received(1).ShowAsync(Arg.Is<BookletSettings>(s =>
            s.Sheet.PaperSize == PaperSize.A4 && s.Layout.ScaleMode == ScaleMode.FitToArea));
    }

    [TestMethod]
    public async Task Export_WritesPlannedBookletToChosenFile()
    {
        var viewModel = await LoadedAsync(pages: 8);
        _files.PickDestinationAsync("novel-booklet.pdf", Arg.Any<string?>()).Returns("/out/result.pdf");

        await viewModel.ExportCommand.ExecuteAsync(null);

        await _exporter.Received(1).ExportAsync(
            Arg.Is<PdfSourceInfo>(s => s.PageCount == 8),
            Arg.Is<BookletPlan>(p => p.Sheets.Count == 2),
            Arg.Any<OutputSettings>(),
            "/out/result.pdf",
            Arg.Any<CancellationToken>());
        await _info.Received(1).ShowInfoAsync("Export complete", Arg.Is<string>(m => m.Contains("/out/result.pdf")));
        _configuration.Received().Apply(Arg.Is<AppConfiguration>(c => c.UiPreferences.LastExportDirectory == "/out"));
    }

    [TestMethod]
    public async Task Export_RefusesToOverwriteTheSource()
    {
        var viewModel = await LoadedAsync(path: "/books/novel.pdf");
        _files.PickDestinationAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns("/books/novel.pdf");

        await viewModel.ExportCommand.ExecuteAsync(null);

        await _exporter.DidNotReceiveWithAnyArgs().ExportAsync(default!, default!, default!, default!, default);
        await _info.Received(1).ShowInfoAsync("Export failed", Arg.Is<string>(m => m.Contains("overwrite")));
    }

    [TestMethod]
    public async Task Export_WhenPickerCancelled_DoesNothing()
    {
        var viewModel = await LoadedAsync();
        _files.PickDestinationAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns((string?)null);

        await viewModel.ExportCommand.ExecuteAsync(null);

        await _exporter.DidNotReceiveWithAnyArgs().ExportAsync(default!, default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Export_Failure_IsReportedAndBusyIsReleased()
    {
        var viewModel = await LoadedAsync();
        _files.PickDestinationAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns("/out/x.pdf");
        _exporter.ExportAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(Task.FromException(new PdfBookletException("disk full")));

        await viewModel.ExportCommand.ExecuteAsync(null);

        await _info.Received(1).ShowInfoAsync("Export failed", "disk full");
        Assert.IsFalse(viewModel.IsBusy);
        Assert.IsTrue(viewModel.OpenPdfCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task SheetNavigation_StaysWithinBounds()
    {
        var viewModel = await LoadedAsync(pages: 8);
        Assert.IsFalse(viewModel.PreviousSheetCommand.CanExecute(null));
        Assert.IsTrue(viewModel.NextSheetCommand.CanExecute(null));

        viewModel.NextSheetCommand.Execute(null);

        Assert.AreEqual(1, viewModel.SheetIndex);
        Assert.IsTrue(viewModel.PreviousSheetCommand.CanExecute(null));
        Assert.IsFalse(viewModel.NextSheetCommand.CanExecute(null));
        Assert.AreEqual("Sheet 2 / 2", viewModel.SheetLabel);
    }

    [TestMethod]
    public void InvalidPersistedSettings_ShowValidationMessageAndNoPlan()
    {
        var viewModel = CreateViewModel();
        _config.Layout.PagesPerSignature = 6;

        var second = new MainWindowViewModel(
            _files, _settingsDialog, _reader,
            new BookletLayoutPlanner(new SheetSizeResolver()),
            new BookletSettingsValidator(new SheetSizeResolver()),
            _exporter, _configuration, _info, NullLogger<MainWindowViewModel>.Instance);

        Assert.IsTrue(second.HasValidationMessage);
        Assert.IsNull(second.Plan);
        Assert.IsNotNull(viewModel);
    }
}
