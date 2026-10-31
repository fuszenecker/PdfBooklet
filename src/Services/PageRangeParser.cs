using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using PdfBooklet.Interfaces;

namespace PdfBooklet.Services;

public sealed class PageRangeParser : IPageRangeParser
{
    private static readonly char[] ItemSeparators = [',', ';'];
    private static readonly char[] RangeSeparators = ['-', '–', '—'];

    public bool TryParse(
        string? text,
        int pageCount,
        [NotNullWhen(true)] out IReadOnlyList<int>? pages,
        [NotNullWhen(false)] out string? error)
    {
        pages = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            pages = Enumerable.Range(0, pageCount).ToList();
            error = null;
            return true;
        }

        var result = new List<int>();
        foreach (var rawItem in text.Split(ItemSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryParseItem(rawItem, pageCount, out var first, out var last, out error))
            {
                return false;
            }

            for (var page = first; page <= last; page++)
            {
                result.Add(page - 1);
            }
        }

        if (result.Count == 0)
        {
            error = "The page selection contains no pages.";
            return false;
        }

        pages = result;
        error = null;
        return true;
    }

    private static bool TryParseItem(string item, int pageCount, out int first, out int last, [NotNullWhen(false)] out string? error)
    {
        first = last = 0;
        error = null;

        var dash = item.IndexOfAny(RangeSeparators);
        if (dash < 0)
        {
            if (!TryParsePage(item, pageCount, out first, out error))
            {
                return false;
            }

            last = first;
            return true;
        }

        var start = item[..dash].Trim();
        var end = item[(dash + 1)..].Trim();
        if (start.Length == 0 && end.Length == 0)
        {
            error = $"\"{item}\" is not a page or a page range.";
            return false;
        }

        if (start.Length == 0)
        {
            first = 1;
        }
        else if (!TryParsePage(start, pageCount, out first, out error))
        {
            return false;
        }

        if (end.Length == 0)
        {
            last = pageCount;
        }
        else if (!TryParsePage(end, pageCount, out last, out error))
        {
            return false;
        }

        if (first > last)
        {
            error = $"The range \"{item}\" ends before it starts.";
            return false;
        }

        return true;
    }

    private static bool TryParsePage(string text, int pageCount, out int page, [NotNullWhen(false)] out string? error)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out page))
        {
            error = $"\"{text}\" is not a page number.";
            return false;
        }

        if (page < 1 || page > pageCount)
        {
            error = $"Page {page} does not exist; the document has {pageCount} {(pageCount == 1 ? "page" : "pages")}.";
            return false;
        }

        error = null;
        return true;
    }
}
