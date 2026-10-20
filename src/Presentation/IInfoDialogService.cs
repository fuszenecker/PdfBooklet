using System.Threading.Tasks;

namespace PdfBooklet.Presentation;

public interface IInfoDialogService
{
    /// <summary>
    /// Shows an informational dialog. The first line of <paramref name="message"/> is the
    /// primary text; any remaining lines are shown in a selectable detail box.
    /// </summary>
    Task ShowInfoAsync(string title, string message);
}
