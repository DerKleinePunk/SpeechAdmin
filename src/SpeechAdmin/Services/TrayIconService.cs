using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Service für System Tray Integration
    /// </summary>
    public class TrayIconService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private Window? _mainWindow;
        private readonly ILogger<TrayIconService> _logger;

        public TrayIconService(ILogger<TrayIconService>? logger = null)
        {
            _logger = logger ?? new NullLogger<TrayIconService>();
        }

        /// <summary>
        /// Initialisiert das Tray-Icon mit dem Hauptfenster
        /// </summary>
        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;
            CreateTrayIcon();
            _logger.LogInformation("Tray-Icon initialisiert");
        }

        private void CreateTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // Hier könnte ein Custom Icon verwendet werden
                Text = "SpeechAdmin - Sprache zu Text",
                Visible = true
            };

            _contextMenu = new ContextMenuStrip();
            var showItem = _contextMenu.Items.Add("Anzeigen");
            showItem.Click += (s, e) => ShowWindow();

            var exitItem = _contextMenu.Items.Add("Beenden");
            exitItem.Click += (s, e) => ExitApplication();

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
            _logger.LogDebug("Kontextmenü für das Tray-Icon erstellt");
        }

        private void ShowWindow()
        {
            if (_mainWindow == null)
                return;

            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            if (_mainWindow.Visibility == Visibility.Hidden)
            {
                _mainWindow.Show();
            }

            _mainWindow.Activate();
            _mainWindow.Focus();
            _logger.LogDebug("Hauptfenster angezeigt");
        }

        private void ExitApplication()
        {
            _logger.LogInformation("Beenden vom Tray-Icon angefordert");
            _mainWindow?.Close();

            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            _logger.LogDebug("TrayIconService disposed");
        }
    }
}