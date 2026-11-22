using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfBooklet.Configuration;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;

namespace PdfBooklet.ViewModels;

/// <summary>
/// Editable copy of the booklet settings. Nothing reaches the live configuration until the
/// user accepts; OK is disabled while the current values fail validation.
/// </summary>
public partial class SettingsDialogViewModel : ViewModelBase
{
    private readonly IBookletSettingsValidator _validator;
    private readonly Func<string?, Task<string?>> _pickApplication;
    private PdfViewerOption _lastRealViewer = PdfViewerOption.SystemDefault;

    public PaperSize[] PaperSizes { get; } = Enum.GetValues<PaperSize>();
    public BindingEdge[] BindingEdges { get; } = Enum.GetValues<BindingEdge>();
    public ScaleMode[] ScaleModes { get; } = Enum.GetValues<ScaleMode>();
    public PageHorizontalAlignment[] HorizontalAlignments { get; } = Enum.GetValues<PageHorizontalAlignment>();
    public PageVerticalAlignment[] VerticalAlignments { get; } = Enum.GetValues<PageVerticalAlignment>();
    public PdfVersion[] PdfVersions { get; } = Enum.GetValues<PdfVersion>();

    /// <summary>System default first, then the viewers found, the custom program (if any), and "Custom…" last.</summary>
    public ObservableCollection<PdfViewerOption> Viewers { get; }

    public IRelayCommand OkCommand { get; }
    public IRelayCommand CancelCommand { get; }

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
    private bool manualDuplex;

    [ObservableProperty]
    private bool openAfterExport;

    [ObservableProperty]
    private PdfViewerOption selectedViewer = PdfViewerOption.SystemDefault;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    public bool IsCustomPaperSize => PaperSize == PaperSize.Custom;
    public bool IsCustomScale => ScaleMode == ScaleMode.Custom;
    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

    /// <summary>Raised with true when the user accepted, false when cancelled.</summary>
    public event EventHandler<bool>? CloseRequested;

    public SettingsDialogViewModel(
        BookletSettings current,
        IBookletSettingsValidator validator,
        IPdfViewerCatalog viewerCatalog,
        Func<string?, Task<string?>> pickApplication)
    {
        _validator = validator;
        _pickApplication = pickApplication;

        OkCommand = new RelayCommand(() => CloseRequested?.Invoke(this, true), () => !HasValidationMessage);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, false));

        PaperSize = current.Sheet.PaperSize;
        CustomWidthMm = current.Sheet.CustomWidthMm;
        CustomHeightMm = current.Sheet.CustomHeightMm;
        BindingEdge = current.Layout.Binding;
        MarginTopMm = current.Layout.MarginTopMm;
        MarginBottomMm = current.Layout.MarginBottomMm;
        MarginInnerMm = current.Layout.MarginInnerMm;
        MarginOuterMm = current.Layout.MarginOuterMm;
        ScaleMode = current.Layout.ScaleMode;
        CustomScalePercent = current.Layout.CustomScalePercent;
        HorizontalAlignment = current.Layout.HorizontalAlignment;
        VerticalAlignment = current.Layout.VerticalAlignment;
        PagesPerSignature = current.Layout.PagesPerSignature;
        PdfVersion = current.Output.PdfVersion;
        CompressStreams = current.Output.CompressStreams;
        ManualDuplex = current.Output.ManualDuplex;
        OpenAfterExport = current.Output.OpenAfterExport;

        Viewers = new ObservableCollection<PdfViewerOption>(
            viewerCatalog.InstalledViewers.Select(v => new PdfViewerOption(PdfViewerOptionKind.Installed, v.Name, v.Path)));
        Viewers.Insert(0, PdfViewerOption.SystemDefault);
        Viewers.Add(PdfViewerOption.Browse);
        SelectedViewer = FindOrAddViewer(current.Output.Viewer);

        Revalidate();
    }

    public BookletSettings Build() => new(
        new SheetSettings
        {
            PaperSize = PaperSize,
            CustomWidthMm = CustomWidthMm,
            CustomHeightMm = CustomHeightMm,
        },
        new LayoutSettings
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
        },
        new OutputSettings
        {
            PdfVersion = PdfVersion,
            CompressStreams = CompressStreams,
            ManualDuplex = ManualDuplex,
            OpenAfterExport = OpenAfterExport,
            Viewer = SelectedViewer.Path,
        });

    partial void OnSelectedViewerChanged(PdfViewerOption value)
    {
        // A ComboBox writes null back when its selected item leaves the list.
        if (value is null)
        {
            SelectedViewer = _lastRealViewer;
        }
        else if (value.Kind == PdfViewerOptionKind.Browse)
        {
            _ = PickCustomViewerAsync();
        }
        else
        {
            _lastRealViewer = value;
        }
    }

    /// <summary>"Custom…" was chosen: pick the program, or go back to the previous choice when cancelled.</summary>
    private async Task PickCustomViewerAsync()
    {
        var previous = _lastRealViewer;
        string? path = null;
        try
        {
            path = await _pickApplication(System.IO.Path.GetDirectoryName(previous.Path ?? string.Empty));
        }
        catch (Exceptions.PdfBookletException)
        {
            // Not a local file: treated like a cancelled picker.
        }

        SelectedViewer = string.IsNullOrWhiteSpace(path) ? previous : FindOrAddViewer(path);
    }

    /// <summary>The entry for <paramref name="path"/>; a program that is not in the list replaces the custom entry.</summary>
    private PdfViewerOption FindOrAddViewer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return PdfViewerOption.SystemDefault;
        }

        var existing = Viewers.FirstOrDefault(v => v.Path is not null && PathsEqual(v.Path, path));
        if (existing is not null)
        {
            return existing;
        }

        var custom = PdfViewerOption.Custom(path);
        var index = Viewers.IndexOf(Viewers.FirstOrDefault(v => v.Kind == PdfViewerOptionKind.Custom) ?? PdfViewerOption.Browse);
        if (Viewers[index].Kind == PdfViewerOptionKind.Custom)
        {
            Viewers[index] = custom;
        }
        else
        {
            Viewers.Insert(index, custom);
        }

        return custom;
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(a, b, OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is not (nameof(ValidationMessage) or nameof(HasValidationMessage) or nameof(IsCustomPaperSize) or nameof(IsCustomScale)
            or nameof(OpenAfterExport) or nameof(SelectedViewer)))
        {
            Revalidate();
        }
    }

    private void Revalidate()
    {
        var settings = Build();
        ValidationMessage = _validator.Validate(settings.Sheet, settings.Layout) ?? string.Empty;
        OkCommand.NotifyCanExecuteChanged();
    }
}
