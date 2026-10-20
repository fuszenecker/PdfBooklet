namespace PdfBooklet.Models;

/// <summary>A page or sheet size in PDF points (1/72 inch).</summary>
public readonly record struct PageSize(double Width, double Height);
