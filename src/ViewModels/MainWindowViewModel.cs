using System;
using System.Collections.Generic;
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
    private static readonly HashSet<string> SettingNames =
    [
        nameof(PaperSize), nameof(CustomWidthMm), nameof(CustomHeightMm),
        nameof(BindingEdge), nameof(MarginTopMm), nameof(MarginBottomMm), nameof(MarginInnerMm), nameof(MarginOuterMm),
        nameof(ScaleMode), nameof(CustomScalePercent), nameof(HorizontalAlignment), nameof(VerticalAlignment),
        nameof(PagesPerSignature), nameof(PdfVersion), nameof(CompressStreams),
    ];

    private readonly IPdfFileDialogService _fileDialogService;
    private readonly IPdfSourceReader _sourceReader;
    private readonly IBookletLayoutPlanner _planner;
    private readonly IBookletSettingsValidator _validator;
    private readonly IPdfBookletExporter _exporter;
    private readonly IConfigurationService _configurationService;
    private readonly IInfoDialogService _infoDialogService;
    private readonly ILogger<MainWindowViewModel> _logger;

    private PdfSourceInfo? _source;
    private bool _isLoadingSettings;

    public PaperSize[] PaperSizes { get; } = Enum.GetValues<PaperSize>();
    public BindingEdge[] BindingEdges { get; } = Enum.GetValues<BindingEdge>();
    public ScaleMode[] ScaleModes { get; } = Enum.GetValues<ScaleMode>();
    public PageHorizontalAlignment[] HorizontalAlignments { get; } = Enum.GetValues<PageHorizontalAlignment>();
    public PageVerticalAlignment[] VerticalAlignments { get; } = Enum.GetValues<PageVerticalAlignment>();
    public PdfVersion[] PdfVersions { get; } = Enum.GetValues<PdfVersion>();

    public IAsyncRelayCommand OpenPdfCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand PreviousSheetCommand { get; }
    public IRelayCommand NextSheetCommand { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomPaperSize))]
    private PaperSize paperSize;

    [ObservableProperty]
    private double customWidthMm;

    [ObservableProperty]
    private double customHeightMm;

    [ObservableProperty]
    private BindingEdge bindingEdge;

    [ObservableProperty]
    private double marginTopMm;

    [ObservableProperty]
    private double marginBottomMm;

    [ObservableProperty]
    private double marginInnerMm;

    [ObservableProperty]
    private double marginOuterMm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomScale))]
    private ScaleMode scaleMode;

    [ObservableProperty]
    private double customScalePercent;

    [ObservableProperty]
    private PageHorizontalAlignment horizontalAlignment;

    [ObservableProperty]
    private PageVerticalAlignment verticalAlignment;

    [ObservableProperty]
    private int pagesPerSignature;

    [ObservableProperty]
    private PdfVersion pdfVersion;

    [ObservableProperty]
    private bool compressStreams;

    [ObservableProperty]
    private string sourceSummary = "No PDF loaded. Use File → Open PDF.";

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

    public bool IsCustomPaperSize => PaperSize == PaperSize.Custom;
    public bool IsCustomScale => ScaleMode == ScaleMode.Custom;
    public bool HasSource => _source is not null;
    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);
    public bool CanInteract => !IsBusy;

    public string SheetLabel => Plan is { Sheets.Count: > 0 } plan
        ? $"Sheet {SheetIndex + 1} / {plan.Sheets.Count}"
        : "No sheets";

    public MainWindowViewModel(
        IPdfFileDialogService fileDialogService,
        IPdfSourceReader sourceReader,
        IBookletLayoutPlanner planner,
        IBookletSettingsValidator validator,
        IPdfBookletExporter exporter,
        IConfigurationService configurationService,
        IInfoDialogService infoDialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _fileDialogService = fileDialogService;
        _sourceReader = sourceReader;
        _planner = planner;
        _validator = validator;
        _exporter = exporter;
        _configurationService = configurationService;
        _infoDialogService = infoDialogService;
        _logger = logger;

        OpenPdfCommand = new AsyncRelayCommand(OpenPdfAsync, () => CanInteract);
        ExportCommand = new AsyncRelayCommand(ExportAsync, CanExport);
        PreviousSheetCommand = new RelayCommand(() => SheetIndex--, () => SheetIndex > 0);
        NextSheetCommand = new RelayCommand(() => SheetIndex++, () => Plan is { } p && SheetIndex < p.Sheets.Count - 1);

        LoadSettings(_configurationService.Current);
        RebuildPlan();
    }

    private bool CanExport() => CanInteract && _source is not null && Plan is not null;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (!_isLoadingSettings && e.PropertyName is not null && SettingNames.Contains(e.PropertyName))
        {
            RebuildPlan();
            PersistSettings();
        }

        switch (e.PropertyName)
        {
            case nameof(IsBusy):
                OpenPdfCommand.NotifyCanExecuteChanged();
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

    private void LoadSettings(AppConfiguration config)
    {
        _isLoadingSettings = true;
        try
        {
            PaperSize = config.Sheet.PaperSize;
            CustomWidthMm = config.Sheet.CustomWidthMm;
            CustomHeightMm = config.Sheet.CustomHeightMm;
            BindingEdge = config.Layout.Binding;
            MarginTopMm = config.Layout.MarginTopMm;
            MarginBottomMm = config.Layout.MarginBottomMm;
            MarginInnerMm = config.Layout.MarginInnerMm;
            MarginOuterMm = config.Layout.MarginOuterMm;
            ScaleMode = config.Layout.ScaleMode;
            CustomScalePercent = config.Layout.CustomScalePercent;
            HorizontalAlignment = config.Layout.HorizontalAlignment;
            VerticalAlignment = config.Layout.VerticalAlignment;
            PagesPerSignature = config.Layout.PagesPerSignature;
            PdfVersion = config.Output.PdfVersion;
            CompressStreams = config.Output.CompressStreams;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private SheetSettings BuildSheetSettings() => new()
    {
        PaperSize = PaperSize,
        CustomWidthMm = CustomWidthMm,
        CustomHeightMm = CustomHeightMm,
    };

    private LayoutSettings BuildLayoutSettings() => new()
    {
        Binding = BindingEdge,
        MarginTopMm = MarginTopMm,
        MarginBottomMm = MarginBottomMm,
        MarginInnerMm = MarginInnerMm,
        MarginOuterMm = MarginOuterMm,
        ScaleMode = ScaleMode,
        CustomScalePercent = CustomScalePercent,
        HorizontalAlignment = HorizontalAlignment,
        VerticalAlignment = VerticalAlignment,
        PagesPerSignature = PagesPerSignature,
    };

    private OutputSettings BuildOutputSettings() => new()
    {
        PdfVersion = PdfVersion,
        CompressStreams = CompressStreams,
    };

    private void PersistSettings()
    {
        var config = _configurationService.Current.Clone();
        config.Sheet = BuildSheetSettings();
        config.Layout = BuildLayoutSettings();
        config.Output = BuildOutputSettings();
        _configurationService.Apply(config);
    }

    private void RebuildPlan()
    {
        var error = _validator.Validate(BuildSheetSettings(), BuildLayoutSettings());
        ValidationMessage = error ?? string.Empty;

        if (_source is null || error is not null)
        {
            Plan = null;
            LayoutSummary = string.Empty;
            SheetIndex = 0;
            return;
        }

        var plan = _planner.Plan(_source, BuildSheetSettings(), BuildLayoutSettings());
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
            var sheet = BuildSheetSettings();
            var layout = BuildLayoutSettings();
            if (_validator.Validate(sheet, layout) is { } error)
            {
                throw new PdfBookletException(error);
            }

            StatusMessage = $"Exporting {Path.GetFileName(destination)}…";
            await _exporter.ExportAsync(_source, _planner.Plan(_source, sheet, layout), BuildOutputSettings(), destination);

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
