using System;
using System.ComponentModel;
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

    public PaperSize[] PaperSizes { get; } = Enum.GetValues<PaperSize>();
    public BindingEdge[] BindingEdges { get; } = Enum.GetValues<BindingEdge>();
    public ScaleMode[] ScaleModes { get; } = Enum.GetValues<ScaleMode>();
    public PageHorizontalAlignment[] HorizontalAlignments { get; } = Enum.GetValues<PageHorizontalAlignment>();
    public PageVerticalAlignment[] VerticalAlignments { get; } = Enum.GetValues<PageVerticalAlignment>();
    public PdfVersion[] PdfVersions { get; } = Enum.GetValues<PdfVersion>();

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
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    public bool IsCustomPaperSize => PaperSize == PaperSize.Custom;
    public bool IsCustomScale => ScaleMode == ScaleMode.Custom;
    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

    /// <summary>Raised with true when the user accepted, false when cancelled.</summary>
    public event EventHandler<bool>? CloseRequested;

    public SettingsDialogViewModel(BookletSettings current, IBookletSettingsValidator validator)
    {
        _validator = validator;

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
        });

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is not (nameof(ValidationMessage) or nameof(HasValidationMessage) or nameof(IsCustomPaperSize) or nameof(IsCustomScale)))
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
