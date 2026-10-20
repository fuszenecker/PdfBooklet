using System;
using System.Threading.Tasks;
using PdfBooklet.Models;
using PdfBooklet.Views;

namespace PdfBooklet.Presentation;

public sealed class SettingsDialogService : ISettingsDialogService
{
    private readonly IWindowContext _windowContext;
    private readonly IDialogViewModelFactory _viewModelFactory;

    public SettingsDialogService(IWindowContext windowContext, IDialogViewModelFactory viewModelFactory)
    {
        _windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
    }

    public async Task<BookletSettings?> ShowAsync(BookletSettings current)
    {
        var owner = _windowContext.ActiveWindow;
        if (owner is null)
        {
            return null;
        }

        var viewModel = _viewModelFactory.CreateSettings(current);
        var accepted = await new SettingsDialog(viewModel).ShowDialog<bool>(owner);
        return accepted ? viewModel.Build() : null;
    }
}
