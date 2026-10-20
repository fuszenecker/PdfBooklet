using PdfBooklet.ViewModels;

namespace PdfBooklet.Presentation;

public sealed class DialogViewModelFactory : IDialogViewModelFactory
{
    public InfoDialogViewModel CreateInfo(string title, string primaryMessage, string detailMessage) =>
        new(title, primaryMessage, detailMessage);
}
