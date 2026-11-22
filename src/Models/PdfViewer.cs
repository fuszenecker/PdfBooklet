namespace PdfBooklet.Models;

/// <summary>An application that can open a PDF: its display name and the executable (or macOS <c>.app</c> bundle).</summary>
public sealed record PdfViewer(string Name, string Path);
