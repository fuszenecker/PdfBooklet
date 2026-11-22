using PdfBooklet.Interfaces;
using PdfBooklet.Models;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Presentation;

public sealed class DialogViewModelFactory : IDialogViewModelFactory
{
    private readonly IBookletSettingsValidator _validator;
    private readonly IPdfViewerCatalog _viewerCatalog;
    private readonly IPdfFileDialogService _fileDialogService;
    private readonly IApplicationInfo _applicationInfo;

    public DialogViewModelFactory(
        IBookletSettingsValidator validator,
        IPdfViewerCatalog viewerCatalog,
        IPdfFileDialogService fileDialogService,
        IApplicationInfo applicationInfo)
    {
        _applicationInfo = applicationInfo;
        _validator = validator;
        _viewerCatalog = viewerCatalog;
        _fileDialogService = fileDialogService;
    }

    public InfoDialogViewModel CreateInfo(string title, string primaryMessage, string detailMessage) =>
        new(title, primaryMessage, detailMessage);

    public SettingsDialogViewModel CreateSettings(BookletSettings current) =>
        new(current, _validator, _viewerCatalog, _fileDialogService.PickApplicationAsync);

    public AboutDialogViewModel CreateAbout() => new(_applicationInfo);
}
