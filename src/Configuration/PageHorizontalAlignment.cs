namespace PdfBooklet.Configuration;

/// <summary>Horizontal page alignment, mirrored on the left and right page of a spread.</summary>
public enum PageHorizontalAlignment
{
    /// <summary>Towards the spine.</summary>
    Inner,
    Center,

    /// <summary>Towards the outer (unbound) edge.</summary>
    Outer,
}
