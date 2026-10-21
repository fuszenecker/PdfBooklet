using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfBooklet.Configuration;
using PdfBooklet.Exceptions;
using PdfBooklet.Interfaces;
using PdfBooklet.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PdfBooklet.Services;

public sealed class PdfBookletExporter : IPdfBookletExporter
{
    public Task ExportAsync(
        PdfSourceInfo source,
        BookletPlan plan,
        OutputSettings output,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        return Task.Run(() => Export(source, plan, output, destinationPath, cancellationToken), cancellationToken);
    }

    private static void Export(
        PdfSourceInfo source,
        BookletPlan plan,
        OutputSettings output,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath))!;
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var document = new PdfDocument())
            {
                document.Version = (int)output.PdfVersion;
                document.Info.Creator = "PdfBooklet";
                document.Options.FlateEncodeMode = output.CompressStreams
                    ? PdfFlateEncodeMode.BestCompression
                    : PdfFlateEncodeMode.Default;
                document.Options.NoCompression = !output.CompressStreams;

                using var form = XPdfForm.FromFile(source.Path);

                foreach (var side in plan.SidesInPrintOrder(output.ManualDuplex))
                {
                    AddSide(document, form, plan.Sheet, side, cancellationToken);
                }

                document.Save(temporaryPath);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or PdfBookletException))
        {
            throw new PdfBookletException($"Could not export the booklet to \"{destinationPath}\": {ex.Message}", ex);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static void AddSide(
        PdfDocument document,
        XPdfForm form,
        PageSize sheetSize,
        SheetSide side,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = document.AddPage();
        page.Width = XUnit.FromPoint(sheetSize.Width);
        page.Height = XUnit.FromPoint(sheetSize.Height);

        using var gfx = XGraphics.FromPdfPage(page);
        Draw(gfx, form, side.Left);
        Draw(gfx, form, side.Right);
    }

    private static void Draw(XGraphics gfx, XPdfForm form, PlacedPage placed)
    {
        if (placed.SourcePageIndex is not { } index)
        {
            return;
        }

        form.PageIndex = index;

        var state = gfx.Save();
        gfx.IntersectClip(new XRect(placed.HalfSheet.X, placed.HalfSheet.Y, placed.HalfSheet.Width, placed.HalfSheet.Height));
        gfx.DrawImage(form, new XRect(placed.Content.X, placed.Content.Y, placed.Content.Width, placed.Content.Height));
        gfx.Restore(state);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup of the temporary file; the export outcome is already decided.
        }
    }
}
