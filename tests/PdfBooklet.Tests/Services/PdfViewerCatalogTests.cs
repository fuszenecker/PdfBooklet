using PdfBooklet.Models;
using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class PdfViewerCatalogTests
{
    private static IReadOnlyList<PdfViewer> Find(HostPlatform platform, Dictionary<string, string> environment, params string[] existing) =>
        new PdfViewerCatalog(platform, name => environment.GetValueOrDefault(name), existing.Contains).InstalledViewers;

    [TestMethod]
    public void Linux_FindsViewersOnThePathInCatalogOrder()
    {
        var viewers = Find(HostPlatform.Linux, new() { ["PATH"] = "/usr/local/bin:/usr/bin" },
            "/usr/bin/okular", "/usr/local/bin/evince", "/usr/bin/evince", "/usr/bin/chromium-browser", "/usr/bin/unrelated");

        CollectionAssert.AreEqual(
            new[] { new PdfViewer("Evince", "/usr/local/bin/evince"), new PdfViewer("Okular", "/usr/bin/okular"), new PdfViewer("Chromium", "/usr/bin/chromium-browser") },
            viewers.ToArray());
    }

    [TestMethod]
    public void Linux_WithoutPath_FindsNothing()
    {
        Assert.IsEmpty(Find(HostPlatform.Linux, new(), "/usr/bin/okular"));
    }

    [TestMethod]
    public void Windows_LooksInTheProgramFolders()
    {
        var viewers = Find(HostPlatform.Windows,
            new() { ["ProgramFiles"] = @"C:\Program Files", ["ProgramFiles(x86)"] = @"C:\Program Files (x86)\", ["LOCALAPPDATA"] = @"C:\Users\u\AppData\Local" },
            @"C:\Users\u\AppData\Local\SumatraPDF\SumatraPDF.exe", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");

        CollectionAssert.AreEqual(
            new[]
            {
                new PdfViewer("SumatraPDF", @"C:\Users\u\AppData\Local\SumatraPDF\SumatraPDF.exe"),
                new PdfViewer("Microsoft Edge", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
            },
            viewers.ToArray());
    }

    [TestMethod]
    public void MacOS_FindsApplicationBundles()
    {
        var viewers = Find(HostPlatform.MacOS, new() { ["HOME"] = "/Users/u" },
            "/System/Applications/Preview.app", "/Users/u/Applications/Skim.app");

        CollectionAssert.AreEqual(
            new[] { new PdfViewer("Preview", "/System/Applications/Preview.app"), new PdfViewer("Skim", "/Users/u/Applications/Skim.app") },
            viewers.ToArray());
    }
}
