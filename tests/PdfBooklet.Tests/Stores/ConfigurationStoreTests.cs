using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfBooklet.Configuration;
using PdfBooklet.Interfaces;
using PdfBooklet.Stores;
using Promptuarium.Serializer;

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
        _userConfig = Path.Combine(_directory, "config", "settings.prmt");
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
    public void BundledDefaults_MatchTheCodeDefaults()
    {
        // The committed src/settings.prmt must be the serialized AppConfiguration defaults;
        // regenerate it when a setting or its default changes.
        var bundled = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, ConfigurationPaths.SettingsFileName));

        CollectionAssert.AreEqual(
            PromptuariumSerializer.Serialize(new AppConfiguration(), ConfigurationFormat.Options),
            bundled);
    }

    [TestMethod]
    public async Task Save_WritesAConfigurationContainer()
    {
        await CreateStore(_userConfig).SaveAsync(new AppConfiguration());

        var bytes = File.ReadAllBytes(_userConfig);
        var loaded = PromptuariumSerializer.Deserialize<AppConfiguration>(bytes, ConfigurationFormat.Options);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(PaperSize.A4, loaded.Sheet.PaperSize);
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
        config.Output.ManualDuplex = true;
        config.UiPreferences.LastSourceDirectory = "/books";

        await store.SaveAsync(config);
        var loaded = store.Load();

        Assert.AreEqual(PaperSize.A3, loaded.Sheet.PaperSize);
        Assert.AreEqual(BindingEdge.Right, loaded.Layout.Binding);
        Assert.AreEqual(16, loaded.Layout.PagesPerSignature);
        Assert.IsFalse(loaded.Output.CompressStreams);
        Assert.IsTrue(loaded.Output.ManualDuplex);
        Assert.AreEqual("/books", loaded.UiPreferences.LastSourceDirectory);
    }

    [TestMethod]
    public async Task Save_LeavesNoTemporaryFiles()
    {
        await CreateStore(_userConfig).SaveAsync(new AppConfiguration());

        CollectionAssert.AreEqual(
            new[] { "settings.prmt" },
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

    [TestMethod]
    public async Task Save_WritesVersion1Containers()
    {
        // PdfBooklet 1.2 and earlier (Promptuarium.Serializer 8.3 and earlier) read only PRMT:CFG1-LE:
        // every setting must keep a type of version 1, even with non-default values.
        var config = new AppConfiguration();
        config.Layout.PagesPerSignature = 16;
        config.Layout.MarginTopMm = 12.25;
        await CreateStore(_userConfig).SaveAsync(config);

        foreach (var bytes in new[]
                 {
                     File.ReadAllBytes(_userConfig),
                     File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, ConfigurationPaths.SettingsFileName)),
                 })
        {
            CollectionAssert.AreEqual("PRMT:CFG1-LE"u8.ToArray(), bytes[1..13]);
        }
    }

    [TestMethod]
    public void Load_ReadsAUserFileWrittenByVersion120()
    {
        // Written by PdfBooklet 1.2.0 (Promptuarium.Serializer 8.1.0, PRMT:CFG1-LE).
        Directory.CreateDirectory(Path.GetDirectoryName(_userConfig)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Stores", "TestData", "settings-1.2.0.prmt"), _userConfig);

        var config = CreateStore(_userConfig).Load();

        Assert.AreEqual(PaperSize.Custom, config.Sheet.PaperSize);
        Assert.AreEqual(420.5, config.Sheet.CustomWidthMm);
        Assert.AreEqual(210, config.Sheet.CustomHeightMm);
        Assert.AreEqual(BindingEdge.Right, config.Layout.Binding);
        Assert.AreEqual(12.25, config.Layout.MarginTopMm);
        Assert.AreEqual(16, config.Layout.PagesPerSignature);
        Assert.AreEqual(ScaleMode.Custom, config.Layout.ScaleMode);
        Assert.AreEqual(95, config.Layout.CustomScalePercent);
        Assert.AreEqual(PdfVersion.Pdf15, config.Output.PdfVersion);
        Assert.IsTrue(config.Output.CompressStreams);
        Assert.IsTrue(config.Output.ManualDuplex);
        Assert.AreEqual("/books", config.UiPreferences.LastSourceDirectory);
        Assert.IsNull(config.UiPreferences.LastExportDirectory);
    }
}
