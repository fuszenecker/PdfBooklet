using System.Runtime.InteropServices;
using PdfBooklet.Configuration;

namespace PdfBooklet.Tests.Configuration;

[TestClass]
public sealed class ConfigurationPathsTests
{
    private static string[] Paths(OSPlatform platform, string? xdg = null) =>
        ConfigurationPaths.GetPerUserConfigPaths(platform, "/home/u", "/roaming", "/local", xdg);

    private static string Expected(params string[] parts) => Path.Combine(parts);

    [TestMethod]
    public void MacOS_UsesApplicationSupport()
    {
        var paths = Paths(OSPlatform.OSX);

        CollectionAssert.AreEqual(
            new[] { Expected("/home/u", "Library", "Application Support", "PdfBooklet", "appsettings.json") },
            paths);
    }

    [TestMethod]
    public void MacOS_IgnoresXdgConfigHome()
    {
        var paths = Paths(OSPlatform.OSX, xdg: "/custom");

        Assert.DoesNotContain("custom", paths[0]);
    }

    [TestMethod]
    public void Linux_UsesXdgConfigHomeWhenSet()
    {
        CollectionAssert.AreEqual(
            new[] { Expected("/custom", "PdfBooklet", "appsettings.json") },
            Paths(OSPlatform.Linux, xdg: "/custom"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Linux_FallsBackToDotConfig(string? xdg)
    {
        CollectionAssert.AreEqual(
            new[] { Expected("/home/u", ".config", "PdfBooklet", "appsettings.json") },
            Paths(OSPlatform.Linux, xdg));
    }

    [TestMethod]
    public void Windows_ReadsRoamingThenLocal_LocalWins()
    {
        var paths = Paths(OSPlatform.Windows);

        CollectionAssert.AreEqual(
            new[]
            {
                Expected("/roaming", "PdfBooklet", "appsettings.json"),
                Expected("/local", "PdfBooklet", "appsettings.json"),
            },
            paths);
    }

    [TestMethod]
    public void UnknownPlatform_HasNoUserLocation()
    {
        Assert.IsEmpty(Paths(OSPlatform.Create("OTHER")));
    }

    [TestMethod]
    public void CurrentPlatform_HasAWritableUserLocation()
    {
        Assert.IsNotNull(ConfigurationPaths.GetPreferredPerUserConfigPath());
        Assert.IsFalse(string.IsNullOrWhiteSpace(ConfigurationPaths.GetAppDataDirectory()));
    }
}
