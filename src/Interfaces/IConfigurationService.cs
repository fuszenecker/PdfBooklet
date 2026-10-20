using System.Threading.Tasks;
using AppConfig = PdfBooklet.Configuration.AppConfiguration;

namespace PdfBooklet.Interfaces;

/// <summary>
/// Single owner of the live, in-memory application configuration and the only component
/// permitted to persist it. Reads go through <see cref="Current"/>; writes replace a whole
/// section through <see cref="Apply"/>, which updates <see cref="Current"/> and schedules a
/// serialized save. Nothing else talks to <see cref="IConfigurationStore"/> directly.
/// </summary>
public interface IConfigurationService
{
    AppConfig Current { get; }

    /// <summary>
    /// Replaces the live configuration contents with a copy of <paramref name="settings"/>
    /// and schedules a save. Must be called on the UI thread.
    /// </summary>
    void Apply(AppConfig settings);

    /// <summary>
    /// Persists the current configuration. Saves are serialized so concurrent or rapid
    /// calls run in order and never interleave writes to the backing file.
    /// </summary>
    Task SaveAsync();

    /// <summary>Waits for all configuration saves requested so far to complete.</summary>
    Task FlushAsync();

    /// <summary>
    /// Fire-and-forget variant of <see cref="SaveAsync"/>; the save is still ordered behind
    /// any in-flight save and failures are logged.
    /// </summary>
    void RequestSave();
}
