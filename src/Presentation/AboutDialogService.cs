using System;
using System.Threading.Tasks;
using PdfBooklet.Views;

namespace PdfBooklet.Presentation;

public sealed class AboutDialogService : IAboutDialogService
{
    private readonly IWindowContext _windowContext;
    private readonly IDialogViewModelFactory _viewModelFactory;

    public AboutDialogService(IWindowContext windowContext, IDialogViewModelFactory viewModelFactory)
    {
        _windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
    }

    public async Task ShowAsync()
    {
        var owner = _windowContext.ActiveWindow;
        if (owner is null)
        {
            return;
        }

        await new AboutDialog(_viewModelFactory.CreateAbout()).ShowDialog(owner);
    }
}
