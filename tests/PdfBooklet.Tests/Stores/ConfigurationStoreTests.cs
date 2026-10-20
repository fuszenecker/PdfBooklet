using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfBooklet.Configuration;
using PdfBooklet.Interfaces;
using PdfBooklet.Stores;

namespace PdfBooklet.Tests.Stores;

[TestClass]
public sealed class ConfigurationStoreTests
{
    private string _directory = string.Empty;
    private string _userConfig = string.Empty;

    [TestInitialize]
    public void CreateDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), "pdfbooklet-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _userConfig = Path.Combine(_directory, "config", "appsettings.json");
    }

    [TestCleanup]
    public void DeleteDirectory() => Directory.Delete(_directory, recursive: true);

    private ConfigurationStore CreateStore(string? preferredPath)
    {
        var paths = Substitute.For<IAppPaths>();
        paths.UserConfigPaths.Returns(preferredPath is null ? [] : new[] { preferredPath });
        paths.PreferredUserConfigPath.Returns(preferredPath);
        return new ConfigurationStore(paths, NullLogger<ConfigurationStore>.Instance);
    }

    [TestMethod]
    public void Load_WithoutUserFile_ReturnsBundledDefaults()
    {
        var config = CreateStore(_userConfig).Load();

        Assert.AreEqual(PaperSize.A4, config.Sheet.PaperSize);
        Assert.AreEqual(ScaleMode.FitToArea, config.Layout.ScaleMode);
        Assert.AreEqual(PdfVersion.Pdf17, config.Output.PdfVersion);
    }

    [TestMethod]
    public async Task SaveThenLoad_RoundTripsAndOverridesDefaults()
    {
        var store = CreateStore(_userConfig);
        var config = new AppConfiguration();
        config.Sheet.PaperSize = PaperSize.A3;
        config.Layout.Binding = BindingEdge.Right;
        config.Layout.PagesPerSignature = 16;
        config.Output.CompressStreams = false;
        config.UiPreferences.LastSourceDirectory = "/books";

        await store.SaveAsync(config);
        var loaded = store.Load();

        Assert.AreEqual(PaperSize.A3, loaded.Sheet.PaperSize);
        Assert.AreEqual(BindingEdge.Right, loaded.Layout.Binding);
        Assert.AreEqual(16, loaded.Layout.PagesPerSignature);
        Assert.IsFalse(loaded.Output.CompressStreams);
        Assert.AreEqual("/books", loaded.UiPreferences.LastSourceDirectory);
    }

    [TestMethod]
    public async Task Save_LeavesNoTemporaryFiles()
    {
        await CreateStore(_userConfig).SaveAsync(new AppConfiguration());

        CollectionAssert.AreEqual(
            new[] { "appsettings.json" },
            Directory.EnumerateFiles(Path.GetDirectoryName(_userConfig)!).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public async Task Save_WithoutUserLocation_IsANoOp()
    {
        await CreateStore(null).SaveAsync(new AppConfiguration());

        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(_directory));
    }

    [TestMethod]
    public async Task ConcurrentSaves_NeverCorruptTheFile()
    {
        var store = CreateStore(_userConfig);

        await Task.WhenAll(Enumerable.Range(1, 20).Select(i =>
        {
            var config = new AppConfiguration();
            config.Layout.MarginTopMm = i;
            return store.SaveAsync(config);
        }));

        Assert.IsGreaterThanOrEqualTo(1, store.Load().Layout.MarginTopMm);
    }
}
