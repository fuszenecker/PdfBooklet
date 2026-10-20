using System.Threading;
using System.Threading.Tasks;
using PdfBooklet.Models;

namespace PdfBooklet.Interfaces;

public interface IPdfSourceReader
{
    /// <exception cref="Exceptions.PdfBookletException">
    /// The file cannot be read, is not a PDF, is password protected, or has no pages.
    /// </exception>
    Task<PdfSourceInfo> ReadAsync(string path, CancellationToken cancellationToken = default);
}
