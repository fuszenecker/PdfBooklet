using Microsoft.Extensions.DependencyInjection;
using PdfBooklet.Interfaces;
using PdfBooklet.Presentation;
using PdfBooklet.Services;
using PdfBooklet.Stores;
using PdfBooklet.ViewModels;

namespace PdfBooklet.Composition;

/// <summary>
/// Composition-root helpers that register the application's service collection by kind —
/// infrastructure, stores, services, view models. Declaration order is not required for
/// correct resolution because every service type is registered exactly once.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Plumbing with no domain identity: filesystem paths and the window context.</summary>
        public void AddInfrastructure()
        {
            services.AddSingleton<IWindowContext, WindowContext>();
            services.AddSingleton<IAppPaths, AppPaths>();
        }

        /// <summary>Persistence surfaces accessed through the service layer.</summary>
        public void AddStores()
        {
            services.AddSingleton<IConfigurationStore, ConfigurationStore>();
        }

        /// <summary>Domain and presentation services that sit between the view models and the stores.</summary>
        public void AddServices()
        {
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<ISheetSizeResolver, SheetSizeResolver>();
            services.AddSingleton<IBookletSettingsValidator, BookletSettingsValidator>();
            services.AddSingleton<IBookletLayoutPlanner, BookletLayoutPlanner>();
            services.AddSingleton<IPdfSourceReader, PdfSourceReader>();
            services.AddSingleton<IPdfBookletExporter, PdfBookletExporter>();
            services.AddSingleton<IPdfFileDialogService, PdfFileDialogService>();
            services.AddSingleton<IInfoDialogService, InfoDialogService>();
            services.AddSingleton<ISettingsDialogService, SettingsDialogService>();
        }

        /// <summary>The factory that constructs dialog view models and the application's root view model.</summary>
        public void AddViewModels()
        {
            services.AddSingleton<IDialogViewModelFactory, DialogViewModelFactory>();
            services.AddSingleton<MainWindowViewModel>();
        }
    }
}
