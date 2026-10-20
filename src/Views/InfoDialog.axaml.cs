using Avalonia.Controls;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Views;

public partial class InfoDialog : Window
{
    public InfoDialog()
        : this(new InfoDialogViewModel("Information", string.Empty, string.Empty))
    {
    }

    public InfoDialog(InfoDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
