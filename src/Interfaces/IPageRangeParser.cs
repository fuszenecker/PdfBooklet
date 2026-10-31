using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PdfBooklet.Interfaces;

public interface IPageRangeParser
{
    /// <summary>
    /// Parses a print-dialog style page selection such as <c>1-4, 7, 10-</c> against a document of
    /// <paramref name="pageCount"/> pages. Page numbers are one-based; the result is zero-based page
    /// indices in the order given. Items are separated by commas or semicolons; <c>a-b</c> is a range,
    /// <c>a-</c> runs to the last page, <c>-b</c> starts at the first. Blank text selects every page.
    /// </summary>
    /// <param name="error">A user-facing description of the problem when parsing fails.</param>
    bool TryParse(
        string? text,
        int pageCount,
        [NotNullWhen(true)] out IReadOnlyList<int>? pages,
        [NotNullWhen(false)] out string? error);
}
