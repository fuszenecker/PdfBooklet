using System.ComponentModel;
using System.Diagnostics;
using PdfBooklet.Exceptions;
using PdfBooklet.Models;
using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class PdfViewerLauncherTests
{
    [TestMethod]
    [DataRow(HostPlatform.Linux, "xdg-open")]
    [DataRow(HostPlatform.MacOS, "open")]
    public void SystemDefault_UsesThePlatformOpener(HostPlatform platform, string opener)
    {
        var startInfo = PdfViewerLauncher.CreateStartInfo(platform, "/out/b.pdf", null);

        Assert.AreEqual(opener, startInfo.FileName);
        CollectionAssert.AreEqual(new[] { "/out/b.pdf" }, startInfo.ArgumentList.ToArray());
        Assert.IsFalse(startInfo.UseShellExecute);
    }

    [TestMethod]
    public void SystemDefault_OnWindows_OpensTheFileThroughTheShell()
    {
        var startInfo = PdfViewerLauncher.CreateStartInfo(HostPlatform.Windows, @"C:\out\b.pdf", " ");

        Assert.AreEqual(@"C:\out\b.pdf", startInfo.FileName);
        Assert.IsTrue(startInfo.UseShellExecute);
    }

    [TestMethod]
    public void Executable_GetsTheFileAsItsOnlyArgument()
    {
        var startInfo = PdfViewerLauncher.CreateStartInfo(HostPlatform.Linux, "/out/my booklet.pdf", "/usr/bin/okular");

        Assert.AreEqual("/usr/bin/okular", startInfo.FileName);
        CollectionAssert.AreEqual(new[] { "/out/my booklet.pdf" }, startInfo.ArgumentList.ToArray());
        Assert.IsFalse(startInfo.UseShellExecute);
    }

    [TestMethod]
    public void MacApplicationBundle_IsOpenedWithOpenA()
    {
        var startInfo = PdfViewerLauncher.CreateStartInfo(HostPlatform.MacOS, "/out/b.pdf", "/Applications/Skim.app/");

        Assert.AreEqual("open", startInfo.FileName);
        CollectionAssert.AreEqual(new[] { "-a", "/Applications/Skim.app/", "/out/b.pdf" }, startInfo.ArgumentList.ToArray());
    }

    [TestMethod]
    public void Open_StartFailure_BecomesAUserFacingException()
    {
        ProcessStartInfo? started = null;
        var launcher = new PdfViewerLauncher(HostPlatform.Linux, info =>
        {
            started = info;
            throw new Win32Exception(2, "No such file or directory");
        });

        var ex = Assert.ThrowsExactly<PdfBookletException>(() => launcher.Open("/out/b.pdf", "/opt/gone/viewer"));

        Assert.AreEqual("/opt/gone/viewer", started?.FileName);
        Assert.Contains("viewer", ex.Message);
        Assert.Contains("No such file or directory", ex.Message);
    }
}
