using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeechAdmin.Configuration;
using SpeechAdmin.Services;
using SpeechAdmin.ViewModels;
using SpeechAdmin.Views;
using System;
using System.IO;
using System.Windows;

namespace SpeechAdmin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TrayIconService? _trayIconService;
        private IAsyncDisposable? _serviceProvider;
        private ILogger<App>? _logger;
        private AppSettings? _appSettings;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _appSettings = new AppSettings();
            configuration.Bind(_appSettings);

            // Setup Dependency Injection and Logging
            var services = new ServiceCollection();
            services.AddSingleton(_appSettings);
            services.AddCustomLogging(_appSettings);
            services.AddSingleton<SpeechModelManagerService>();
            services.AddSingleton<AudioRecorderService>();
            services.AddSingleton<KeyboardSimulatorService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<TrayIconService>();

            _serviceProvider = services.BuildServiceProvider();
            _logger = ((IServiceProvider)_serviceProvider).GetRequiredService<ILogger<App>>();

            _logger.LogInformation("SpeechAdmin application started");
            _logger.LogInformation("Configuration loaded from {ConfigPath}", Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));

            // Initialize main window and view model
            Current.MainWindow = new MainWindow(_appSettings);
            var viewModel = ((IServiceProvider)_serviceProvider).GetRequiredService<MainViewModel>();
            Current.MainWindow.DataContext = viewModel;

            // Initialize system tray icon
            _trayIconService = ((IServiceProvider)_serviceProvider).GetRequiredService<TrayIconService>();
            _trayIconService.Initialize(Current.MainWindow);

            // Minimize on close instead of exit
            Current.MainWindow.Closing += (s, args) =>
            {
                if (Current.MainWindow.WindowState != WindowState.Minimized)
                {
                    Current.MainWindow.WindowState = WindowState.Minimized;
                    args.Cancel = true;
                }
            };

            // minimize to tray on button click
            Current.MainWindow.StateChanged += (s, args) =>
            {
                if (Current.MainWindow.WindowState == WindowState.Minimized)
                {
                    Current.MainWindow.Hide();
                }
            };

            // Show the main window
            Current.MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.LogInformation("SpeechAdmin application closed");
            _trayIconService?.Dispose();
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }
    }
}