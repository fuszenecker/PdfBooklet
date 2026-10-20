using System.Threading.Tasks;

namespace PdfBooklet.Presentation;

public interface IPdfFileDialogService
{
    /// <summary>Lets the user pick the PDF to turn into a booklet; null when cancelled.</summary>
    Task<string?> PickSourceAsync(string? startDirectory);

    /// <summary>Lets the user choose where to save the booklet; null when cancelled.</summary>
    Task<string?> PickDestinationAsync(string suggestedFileName, string? startDirectory);
}
