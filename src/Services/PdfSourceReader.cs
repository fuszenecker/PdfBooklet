using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfBooklet.Exceptions;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;
using PdfSharp.Pdf.IO;

namespace PdfBooklet.Services;

public sealed class PdfSourceReader : IPdfSourceReader
{
    public Task<PdfSourceInfo> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Task.Run(() => Read(path, cancellationToken), cancellationToken);
    }

    private static PdfSourceInfo Read(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);

            var pages = new List<PageSize>(document.PageCount);
            foreach (var page in document.Pages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double width = page.Width.Point;
                double height = page.Height.Point;
                if (page.Rotate is 90 or 270)
                {
                    (width, height) = (height, width);
                }

                pages.Add(new PageSize(width, height));
            }

            if (pages.Count == 0)
            {
                throw new PdfBookletException("The PDF has no pages.");
            }

            return new PdfSourceInfo(Path.GetFullPath(path), pages);
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or PdfBookletException))
        {
            throw new PdfBookletException($"Could not read the PDF \"{Path.GetFileName(path)}\": {ex.Message}", ex);
        }
    }
}
