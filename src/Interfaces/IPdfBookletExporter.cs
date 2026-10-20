using System.Threading;
using System.Threading.Tasks;
using PdfBooklet.Configuration;
using PdfBooklet.Models;

namespace PdfBooklet.Interfaces;

public interface IPdfBookletExporter
{
    /// <summary>
    /// Writes the planned booklet to <paramref name="destinationPath"/>. The file is written
    /// to a temporary sibling first and moved into place, so a failure never leaves a
    /// truncated destination.
    /// </summary>
    /// <exception cref="Exceptions.PdfBookletException">The booklet could not be produced or written.</exception>
    Task ExportAsync(
        PdfSourceInfo source,
        BookletPlan plan,
        OutputSettings output,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
