using PdfBooklet.Interfaces;
using PdfBooklet.Models;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Presentation;

public sealed class DialogViewModelFactory : IDialogViewModelFactory
{
    private readonly IBookletSettingsValidator _validator;

    public DialogViewModelFactory(IBookletSettingsValidator validator)
    {
        _validator = validator;
    }

    public InfoDialogViewModel CreateInfo(string title, string primaryMessage, string detailMessage) =>
        new(title, primaryMessage, detailMessage);

    public SettingsDialogViewModel CreateSettings(BookletSettings current) =>
        new(current, _validator);
}
