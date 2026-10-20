using System;
using System.Threading.Tasks;
using PdfBooklet.Views;

namespace PdfBooklet.Presentation;

public sealed class InfoDialogService : IInfoDialogService
{
    private readonly IWindowContext _windowContext;
    private readonly IDialogViewModelFactory _viewModelFactory;

    public InfoDialogService(IWindowContext windowContext, IDialogViewModelFactory viewModelFactory)
    {
        _windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        var owner = _windowContext.ActiveWindow;
        if (owner is null)
        {
            return;
        }

        var separatorIndex = message.IndexOf('\n');
        var (primary, detail) = separatorIndex < 0
            ? (message, string.Empty)
            : (message[..separatorIndex].TrimEnd(), message[(separatorIndex + 1)..].Trim());

        var viewModel = _viewModelFactory.CreateInfo(title, primary, detail);
        await new InfoDialog(viewModel).ShowDialog(owner);
    }
}
