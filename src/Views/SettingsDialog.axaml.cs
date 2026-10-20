using Avalonia.Controls;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
    }

    public SettingsDialog(SettingsDialogViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += (_, accepted) => Close(accepted);
    }
}
