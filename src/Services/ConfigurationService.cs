using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PdfBooklet.Exceptions;
using PdfBooklet.Interfaces;
using AppConfig = PdfBooklet.Configuration.AppConfiguration;

namespace PdfBooklet.Services;

public sealed class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationStore _store;
    private readonly ILogger<ConfigurationService> _logger;

    // Serializes saves: each SaveAsync chains onto the previous one so that rapid
    // or concurrent callers persist in order and never interleave file writes.
    private readonly object _saveGate = new();
    private Task _saveChain = Task.CompletedTask;

    public AppConfig Current { get; }

    public ConfigurationService(IConfigurationStore store, ILogger<ConfigurationService> logger)
    {
        _store = store;
        _logger = logger;
        Current = _store.Load();
    }

    public void Apply(AppConfig settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var copy = settings.Clone();
        Current.Sheet = copy.Sheet;
        Current.Layout = copy.Layout;
        Current.Output = copy.Output;
        Current.UiPreferences = copy.UiPreferences;
        RequestSave();
    }

    public Task SaveAsync()
    {
        // Snapshot on the CALLER's thread (the UI thread, where all mutations happen)
        // so the clone can never race with a concurrent mutation of Current.
        var snapshot = Current.Clone();

        lock (_saveGate)
        {
            _saveChain = _saveChain.ContinueWith(
                _ => PersistAsync(snapshot),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default).Unwrap();

            return _saveChain;
        }
    }

    public void RequestSave()
    {
        // Fire-and-forget callers cannot surface a persistence error. Observe it here;
        // awaited SaveAsync and FlushAsync retain the failure for their caller.
        _ = ObserveBackgroundSaveAsync();
    }

    public Task FlushAsync()
    {
        lock (_saveGate)
        {
            return _saveChain;
        }
    }

    private async Task PersistAsync(AppConfig snapshot)
    {
        try
        {
            await _store.SaveAsync(snapshot).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist configuration");
            throw new ConfigurationPersistenceException("Could not save application settings.", ex);
        }
    }

    private async Task ObserveBackgroundSaveAsync()
    {
        try
        {
            await SaveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "A background configuration save failed");
        }
    }
}
