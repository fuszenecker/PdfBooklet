using System;
using CommunityToolkit.Mvvm.Input;

namespace PdfBooklet.ViewModels;

public sealed class InfoDialogViewModel : ViewModelBase
{
    public string Title { get; }
    public string PrimaryMessage { get; }
    public string DetailMessage { get; }
    public bool HasDetail => !string.IsNullOrEmpty(DetailMessage);

    public IRelayCommand OkCommand { get; }

    public event EventHandler? CloseRequested;

    public InfoDialogViewModel(string title, string primaryMessage, string detailMessage)
    {
        Title = title;
        PrimaryMessage = primaryMessage;
        DetailMessage = detailMessage;
        OkCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }
}
