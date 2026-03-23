using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SpeechAdmin.Ui;
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

            // Apply Dark Mode styling if Windows is in Dark Mode
            ApplyDarkModeToContextMenu(_contextMenu);

            var showItem = _contextMenu.Items.Add("Anzeigen");
            showItem.Click += (s, e) => ShowWindow();

            var exitItem = _contextMenu.Items.Add("Beenden");
            exitItem.Click += (s, e) => ExitApplication();

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
            _logger.LogDebug("Kontextmenü für das Tray-Icon erstellt");
        }

        /// <summary>
        /// Applies Dark Mode styling to the context menu if Windows is in Dark Mode
        /// </summary>
        private void ApplyDarkModeToContextMenu(ContextMenuStrip menu)
        {
            try
            {
                if (IsWindowsInDarkMode())
                {
                    // Dark mode colors
                    menu.Renderer = new ToolStripProfessionalRenderer(new DarkModeColorTable());
                    menu.BackColor = Color.FromArgb(45, 45, 48);
                    menu.ForeColor = Color.White;

                    _logger.LogDebug("Dark Mode für Kontextmenü aktiviert");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fehler beim Anwenden des Dark Mode: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Checks if Windows is currently in Dark Mode
        /// </summary>
        private static bool IsWindowsInDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is 0;
            }
            catch
            {
                return false;
            }
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