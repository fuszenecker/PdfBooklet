using System;

namespace PdfBooklet.Exceptions;

/// <summary>
/// A failure the user can act on (unreadable or encrypted source, export that could not be
/// written). The message is suitable for showing verbatim in a dialog.
/// </summary>
public sealed class PdfBookletException : Exception
{
    public PdfBookletException(string message) : base(message)
    {
    }

    public PdfBookletException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
