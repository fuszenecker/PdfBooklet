using System.Collections.Generic;
using PdfBooklet.Models;

namespace PdfBooklet.Interfaces;

public interface IPdfViewerCatalog
{
    /// <summary>The well-known PDF viewers installed on this computer, in a stable order; may be empty.</summary>
    IReadOnlyList<PdfViewer> InstalledViewers { get; }
}
