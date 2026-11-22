using System.Threading.Tasks;

namespace PdfBooklet.Presentation;

public interface IAboutDialogService
{
    /// <summary>Shows the About dialog: version, copyright, license and links.</summary>
    Task ShowAsync();
}
