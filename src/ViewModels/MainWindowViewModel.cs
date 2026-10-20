using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PdfBooklet.Configuration;
using PdfBooklet.Exceptions;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;
using PdfBooklet.Presentation;
using PdfBooklet.Services;

namespace PdfBooklet.ViewModels;

/// <summary>
/// Shell view model: loads the source PDF, owns the editable booklet settings, keeps the
/// layout plan (and thus the preview) in step with them, and drives the export.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPdfFileDialogService _fileDialogService;
    private readonly ISettingsDialogService _settingsDialogService;
    private readonly IPdfSourceReader _sourceReader;
    private readonly IBookletLayoutPlanner _planner;
    private readonly IBookletSettingsValidator _validator;
    private readonly IPdfBookletExporter _exporter;
    private readonly IConfigurationService _configurationService;
    private readonly IInfoDialogService _infoDialogService;
    private readonly ILogger<MainWindowViewModel> _logger;

    private PdfSourceInfo? _source;
    private BookletSettings _settings;

    public IAsyncRelayCommand OpenPdfCommand { get; }
    public IAsyncRelayCommand OpenSettingsCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand PreviousSheetCommand { get; }
    public IRelayCommand NextSheetCommand { get; }

    [ObservableProperty]
    private string sourceSummary = "No PDF loaded. Use the import button.";

    [ObservableProperty]
    private string layoutSummary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private BookletPlan? plan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SheetLabel))]
    private int sheetIndex;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    private bool isBusy;

    public bool HasSource => _source is not null;
    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);
    public bool CanInteract => !IsBusy;

    public string SheetLabel => Plan is { Sheets.Count: > 0 } plan
        ? $"Sheet {SheetIndex + 1} / {plan.Sheets.Count}"
        : "No sheets";

    public MainWindowViewModel(
        IPdfFileDialogService fileDialogService,
        ISettingsDialogService settingsDialogService,
        IPdfSourceReader sourceReader,
        IBookletLayoutPlanner planner,
        IBookletSettingsValidator validator,
        IPdfBookletExporter exporter,
        IConfigurationService configurationService,
        IInfoDialogService infoDialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _fileDialogService = fileDialogService;
        _settingsDialogService = settingsDialogService;
        _sourceReader = sourceReader;
        _planner = planner;
        _validator = validator;
        _exporter = exporter;
        _configurationService = configurationService;
        _infoDialogService = infoDialogService;
        _logger = logger;

        OpenPdfCommand = new AsyncRelayCommand(OpenPdfAsync, () => CanInteract);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync, () => CanInteract);
        ExportCommand = new AsyncRelayCommand(ExportAsync, CanExport);
        PreviousSheetCommand = new RelayCommand(() => SheetIndex--, () => SheetIndex > 0);
        NextSheetCommand = new RelayCommand(() => SheetIndex++, () => Plan is { } p && SheetIndex < p.Sheets.Count - 1);

        var config = _configurationService.Current;
        _settings = new BookletSettings(config.Sheet.Clone(), config.Layout.Clone(), config.Output.Clone());
        RebuildPlan();
    }

    private bool CanExport() => CanInteract && _source is not null && Plan is not null;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(IsBusy):
                OpenPdfCommand.NotifyCanExecuteChanged();
                OpenSettingsCommand.NotifyCanExecuteChanged();
                ExportCommand.NotifyCanExecuteChanged();
                break;
            case nameof(Plan):
                ExportCommand.NotifyCanExecuteChanged();
                NextSheetCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SheetLabel));
                break;
            case nameof(SheetIndex):
                PreviousSheetCommand.NotifyCanExecuteChanged();
                NextSheetCommand.NotifyCanExecuteChanged();
                break;
        }
    }

    private async Task OpenSettingsAsync()
    {
        var result = await _settingsDialogService.ShowAsync(_settings);
        if (result is null)
        {
            return;
        }

        _settings = result;

        var config = _configurationService.Current.Clone();
        config.Sheet = result.Sheet.Clone();
        config.Layout = result.Layout.Clone();
        config.Output = result.Output.Clone();
        _configurationService.Apply(config);

        RebuildPlan();
    }

    private void RebuildPlan()
    {
        var error = _validator.Validate(_settings.Sheet, _settings.Layout);
        ValidationMessage = error ?? string.Empty;

        if (_source is null || error is not null)
        {
            Plan = null;
            LayoutSummary = string.Empty;
            SheetIndex = 0;
            return;
        }

        var plan = _planner.Plan(_source, _settings.Sheet, _settings.Layout);
        Plan = plan;
        SheetIndex = Math.Clamp(SheetIndex, 0, plan.Sheets.Count - 1);
        LayoutSummary = Describe(_source, plan);
    }

    private static string Describe(PdfSourceInfo source, BookletPlan plan)
    {
        var placements = plan.Sheets
            .SelectMany(sheet => new[] { sheet.Front.Left, sheet.Front.Right, sheet.Back.Left, sheet.Back.Right })
            .ToList();
        var blanks = placements.Count(p => p.SourcePageIndex is null);
        var firstPageScale = placements.First(p => p.SourcePageIndex == 0).Scale;

        var text = $"{source.PageCount} pages → {plan.Sheets.Count} sheets ({plan.SideCount} printed sides)";
        if (blanks > 0)
        {
            text += $", {blanks} blank {(blanks == 1 ? "page" : "pages")} added";
        }

        return $"{text}. Scale of page 1: {firstPageScale * 100:0.#} %.";
    }

    private async Task OpenPdfAsync()
    {
        IsBusy = true;
        try
        {
            var path = await _fileDialogService.PickSourceAsync(_configurationService.Current.UiPreferences.LastSourceDirectory);
            if (path is not null)
            {
                await LoadSourceCoreAsync(path);
            }
        }
        catch (PdfBookletException ex)
        {
            await FailAsync("Open PDF", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Loads <paramref name="path"/> as the source PDF, reporting failures in a dialog.</summary>
    public async Task LoadSourceAsync(string path)
    {
        IsBusy = true;
        try
        {
            await LoadSourceCoreAsync(path);
        }
        catch (PdfBookletException ex)
        {
            await FailAsync("Open PDF", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSourceCoreAsync(string path)
    {
        StatusMessage = $"Reading {Path.GetFileName(path)}…";
        _source = await _sourceReader.ReadAsync(path);
        OnPropertyChanged(nameof(HasSource));

        var first = _source.Pages[0];
        SourceSummary = $"{Path.GetFileName(path)}\n{_source.PageCount} pages, first page {ToMm(first.Width):0.#} × {ToMm(first.Height):0.#} mm";
        RememberDirectory(path, source: true);
        SheetIndex = 0;
        RebuildPlan();
        StatusMessage = $"Loaded {Path.GetFileName(path)}";
    }

    private async Task ExportAsync()
    {
        if (_source is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var suggested = $"{Path.GetFileNameWithoutExtension(_source.Path)}-booklet.pdf";
            var startDirectory = _configurationService.Current.UiPreferences.LastExportDirectory
                ?? Path.GetDirectoryName(_source.Path);

            var destination = await _fileDialogService.PickDestinationAsync(suggested, startDirectory);
            if (destination is null)
            {
                return;
            }

            if (string.Equals(Path.GetFullPath(destination), _source.Path, StringComparison.Ordinal))
            {
                throw new PdfBookletException("The booklet cannot overwrite the source PDF. Choose a different file name.");
            }

            // Re-plan against the exact settings in effect now; the preview plan may be older
            // than a setting the user changed while the file picker was open.
            var sheet = _settings.Sheet;
            var layout = _settings.Layout;
            if (_validator.Validate(sheet, layout) is { } error)
            {
                throw new PdfBookletException(error);
            }

            StatusMessage = $"Exporting {Path.GetFileName(destination)}…";
            await _exporter.ExportAsync(_source, _planner.Plan(_source, sheet, layout), _settings.Output, destination);

            RememberDirectory(destination, source: false);
            StatusMessage = $"Exported {Path.GetFileName(destination)}";
            await _infoDialogService.ShowInfoAsync("Export complete", $"The booklet was saved.\n{destination}");
        }
        catch (PdfBookletException ex)
        {
            await FailAsync("Export failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task FailAsync(string title, PdfBookletException ex)
    {
        _logger.LogWarning(ex, "{Title}", title);
        StatusMessage = title;
        await _infoDialogService.ShowInfoAsync(title, ex.Message);
    }

    private void RememberDirectory(string path, bool source)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        var config = _configurationService.Current.Clone();
        if (source)
        {
            config.UiPreferences.LastSourceDirectory = directory;
        }
        else
        {
            config.UiPreferences.LastExportDirectory = directory;
        }

        _configurationService.Apply(config);
    }

    private static double ToMm(double points) => points / SheetSizeResolver.PointsPerMillimeter;
}
