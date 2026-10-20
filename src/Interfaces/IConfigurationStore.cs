using System.Threading.Tasks;
using AppConfig = PdfBooklet.Configuration.AppConfiguration;

namespace PdfBooklet.Interfaces;

public interface IConfigurationStore
{
    AppConfig Load();

    /// <summary>
    /// Persists the given configuration. The caller must pass an isolated snapshot
    /// (not a live, concurrently-mutated instance); the store does not clone it.
    /// </summary>
    Task SaveAsync(AppConfig configuration);
}
