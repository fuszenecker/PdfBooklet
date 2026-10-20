using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PdfBooklet.Composition;
using PdfBooklet.Interfaces;
using PdfBooklet.Presentation;
using PdfBooklet.ViewModels;
using PdfBooklet.Views;

namespace PdfBooklet;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnDesktopExit;

            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            if (desktop.Args is [var path, ..] && File.Exists(path))
            {
                mainWindow.Opened += async (_, _) => await viewModel.LoadSourceAsync(path);
            }

            _serviceProvider.GetRequiredService<IWindowContext>().MainWindow = mainWindow;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_serviceProvider is null)
        {
            return;
        }

        try
        {
            // Block briefly so the final pending configuration save completes before the
            // process exits. The save chain runs on the threadpool, so blocking the UI thread
            // here cannot deadlock it; the timeout bounds shutdown even if the store hangs.
            _serviceProvider.GetRequiredService<IConfigurationService>()
                .FlushAsync()
                .Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Best-effort: a failed or timed-out flush must not prevent shutdown.
        }
        finally
        {
            _serviceProvider.Dispose();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();

        services.AddInfrastructure();
        services.AddStores();
        services.AddServices();
        services.AddViewModels();
    }
}
