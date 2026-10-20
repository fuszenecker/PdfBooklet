using System.Threading.Tasks;
using PdfBooklet.Models;

namespace PdfBooklet.Presentation;

public interface ISettingsDialogService
{
    /// <summary>
    /// Shows the settings dialog pre-filled with <paramref name="current"/>. Returns the
    /// validated new settings, or null when the user cancelled.
    /// </summary>
    Task<BookletSettings?> ShowAsync(BookletSettings current);
}
