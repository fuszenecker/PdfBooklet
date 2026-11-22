using Avalonia.Controls;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    public AboutDialog(AboutDialogViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
