using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfBooklet.Configuration;
using PdfBooklet.Exceptions;
using PdfBooklet.Interfaces;
using PdfBooklet.Services;

namespace PdfBooklet.Tests.Services;

[TestClass]
public sealed class ConfigurationServiceTests
{
    private static (ConfigurationService Service, IConfigurationStore Store, List<AppConfiguration> Saved) Create()
    {
        var store = Substitute.For<IConfigurationStore>();
        store.Load().Returns(new AppConfiguration());
        var saved = new List<AppConfiguration>();
        store.SaveAsync(Arg.Do<AppConfiguration>(saved.Add)).Returns(Task.CompletedTask);
        return (new ConfigurationService(store, NullLogger<ConfigurationService>.Instance), store, saved);
    }

    [TestMethod]
    public void Constructor_LoadsFromStore()
    {
        var store = Substitute.For<IConfigurationStore>();
        store.Load().Returns(new AppConfiguration { Layout = new LayoutSettings { PagesPerSignature = 8 } });

        var service = new ConfigurationService(store, NullLogger<ConfigurationService>.Instance);

        Assert.AreEqual(8, service.Current.Layout.PagesPerSignature);
    }

    [TestMethod]
    public async Task Apply_ReplacesSectionsAndPersistsOnce()
    {
        var (service, _, saved) = Create();
        var settings = new AppConfiguration();
        settings.Sheet.PaperSize = PaperSize.Legal;
        settings.UiPreferences.LastExportDirectory = "/out";

        service.Apply(settings);
        await service.FlushAsync();

        Assert.AreEqual(PaperSize.Legal, service.Current.Sheet.PaperSize);
        Assert.AreEqual("/out", service.Current.UiPreferences.LastExportDirectory);
        Assert.HasCount(1, saved);
        Assert.AreEqual(PaperSize.Legal, saved[0].Sheet.PaperSize);
    }

    [TestMethod]
    public void Apply_CopiesTheInput()
    {
        var (service, _, _) = Create();
        var settings = new AppConfiguration();
        settings.Layout.MarginTopMm = 5;

        service.Apply(settings);
        settings.Layout.MarginTopMm = 99;

        Assert.AreEqual(5, service.Current.Layout.MarginTopMm);
    }

    [TestMethod]
    public void Apply_RejectsNull()
    {
        var (service, _, _) = Create();

        Assert.ThrowsExactly<ArgumentNullException>(() => service.Apply(null!));
    }

    [TestMethod]
    public async Task Saves_RunInTheOrderTheyWereRequested()
    {
        var store = Substitute.For<IConfigurationStore>();
        store.Load().Returns(new AppConfiguration());
        var service = new ConfigurationService(store, NullLogger<ConfigurationService>.Instance);

        var started = new List<int>();
        var gate = new TaskCompletionSource();
        store.SaveAsync(Arg.Any<AppConfiguration>()).Returns(call =>
        {
            started.Add((int)call.Arg<AppConfiguration>().Layout.MarginTopMm);
            return started.Count == 1 ? gate.Task : Task.CompletedTask;
        });

        service.Current.Layout.MarginTopMm = 4;
        var first = service.SaveAsync();
        service.Current.Layout.MarginTopMm = 8;
        var second = service.SaveAsync();

        await Task.Delay(50);
        CollectionAssert.AreEqual(new[] { 4 }, started, "the second save must wait for the first");
        gate.SetResult();
        await Task.WhenAll(first, second);

        CollectionAssert.AreEqual(new[] { 4, 8 }, started);
    }

    [TestMethod]
    public async Task SaveAsync_SnapshotsStateAtCallTime()
    {
        var (service, _, saved) = Create();

        service.Current.Layout.MarginTopMm = 11;
        var task = service.SaveAsync();
        service.Current.Layout.MarginTopMm = 22;
        await task;

        Assert.AreEqual(11, saved[0].Layout.MarginTopMm);
    }

    [TestMethod]
    public async Task SaveAsync_WrapsStoreFailures()
    {
        var (service, store, _) = Create();
        store.SaveAsync(Arg.Any<AppConfiguration>()).Returns(Task.FromException(new IOException("disk full")));

        var ex = await Assert.ThrowsExactlyAsync<ConfigurationPersistenceException>(() => service.SaveAsync());

        Assert.IsInstanceOfType<IOException>(ex.InnerException);
    }

    [TestMethod]
    public async Task RequestSave_SwallowsFailuresButFlushStillSurfacesThem()
    {
        var (service, store, _) = Create();
        store.SaveAsync(Arg.Any<AppConfiguration>()).Returns(Task.FromException(new IOException("disk full")));

        service.RequestSave();

        await Assert.ThrowsExactlyAsync<ConfigurationPersistenceException>(() => service.FlushAsync());
    }
}
