using System.Collections.Generic;

namespace PdfBooklet.Models;

/// <summary>What the booklet planner needs to know about the loaded PDF.</summary>
/// <param name="Path">Full path of the source file.</param>
/// <param name="Pages">Size of every page as displayed (page rotation applied), in points.</param>
public sealed record PdfSourceInfo(string Path, IReadOnlyList<PageSize> Pages)
{
    public int PageCount => Pages.Count;
}
