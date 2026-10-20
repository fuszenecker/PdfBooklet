using PdfBooklet.ViewModels;

namespace PdfBooklet.Presentation;

public interface IDialogViewModelFactory
{
    InfoDialogViewModel CreateInfo(string title, string primaryMessage, string detailMessage);
}
