namespace PdfBooklet.Models;

/// <summary>A rectangle in PDF points with its origin at the top-left corner of the sheet.</summary>
public readonly record struct PageRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
}
