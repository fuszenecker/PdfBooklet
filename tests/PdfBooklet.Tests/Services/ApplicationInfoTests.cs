using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class ApplicationInfoTests
{
    [TestMethod]
    public void Version_IsTheOneInVersionTxt()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "version.txt")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "version.txt not found above the test output directory");
        var expected = File.ReadAllText(Path.Combine(directory.FullName, "version.txt")).Trim();

        var info = new ApplicationInfo();

        Assert.AreEqual(expected, info.Version);
        Assert.StartsWith(info.ShortVersion, expected);
        Assert.Contains("Fuszenecker", info.Copyright);
        Assert.Contains(".NET", info.Runtime);
    }

    [TestMethod]
    [DataRow("1.3.0.0+a18aebd396d0", "1.3.0.0")]
    [DataRow("1.3.0.0", "1.3.0.0")]
    [DataRow(null, "1.2.0.0")]
    [DataRow("", "1.2.0.0")]
    public void ParseVersion_DropsTheSourceRevision(string? informational, string expected)
    {
        Assert.AreEqual(expected, ApplicationInfo.ParseVersion(informational, new Version(1, 2, 0, 0)));
    }

    [TestMethod]
    [DataRow("1.3.0.0", "1.3.0")]
    [DataRow("1.3.2.7", "1.3.2")]
    [DataRow("1.3.0", "1.3.0")]
    [DataRow("1.3.0-beta", "1.3.0-beta")]
    public void Shorten_DropsTheBuildNumber(string version, string expected)
    {
        Assert.AreEqual(expected, ApplicationInfo.Shorten(version));
    }
}
