using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using PdfBooklet.Exceptions;

namespace PdfBooklet.Presentation;

public sealed class PdfFileDialogService : IPdfFileDialogService
{
    private static readonly FilePickerFileType PdfFileType = new("PDF document")
    {
        Patterns = ["*.pdf"],
        MimeTypes = ["application/pdf"],
    };

    private readonly IWindowContext _windowContext;

    public PdfFileDialogService(IWindowContext windowContext)
    {
        _windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
    }

    public async Task<string?> PickSourceAsync(string? startDirectory)
    {
        var owner = _windowContext.MainWindow;
        if (owner is null)
        {
            return null;
        }

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF",
            AllowMultiple = false,
            FileTypeFilter = [PdfFileType],
            SuggestedStartLocation = await ResolveFolderAsync(owner.StorageProvider, startDirectory),
        });

        return ToLocalPath(files.FirstOrDefault());
    }

    public async Task<string?> PickDestinationAsync(string suggestedFileName, string? startDirectory)
    {
        var owner = _windowContext.MainWindow;
        if (owner is null)
        {
            return null;
        }

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export booklet",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            ShowOverwritePrompt = true,
            FileTypeChoices = [PdfFileType],
            SuggestedStartLocation = await ResolveFolderAsync(owner.StorageProvider, startDirectory),
        });

        return ToLocalPath(file);
    }

    private static async Task<IStorageFolder?> ResolveFolderAsync(IStorageProvider provider, string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        return await provider.TryGetFolderFromPathAsync(directory);
    }

    private static string? ToLocalPath(IStorageItem? item)
    {
        if (item is null)
        {
            return null;
        }

        return item.TryGetLocalPath()
            ?? throw new PdfBookletException("The selected location is not a local file, which PdfBooklet requires.");
    }
}
