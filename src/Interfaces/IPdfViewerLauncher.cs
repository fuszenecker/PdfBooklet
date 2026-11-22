namespace PdfBooklet.Interfaces;

public interface IPdfViewerLauncher
{
    /// <summary>
    /// Opens <paramref name="pdfPath"/> in <paramref name="viewer"/> (an executable or macOS <c>.app</c> bundle), or in
    /// the system's default PDF application when <paramref name="viewer"/> is null or blank. Does not wait for it.
    /// </summary>
    /// <exception cref="Exceptions.PdfBookletException">The application could not be started.</exception>
    void Open(string pdfPath, string? viewer);
}
