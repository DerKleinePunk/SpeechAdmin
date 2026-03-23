using Microsoft.Extensions.Logging;
using SpeechAdmin.Configuration;
using SpeechAdmin.Services;
using SpeechAdmin.ViewModels;
using System;
using System.Windows;
using System.Windows.Interop;

namespace SpeechAdmin.Views
{
    /// <summary>
    /// MainWindow.xaml code behind
    /// </summary>
    public partial class MainWindow : Window
    {
        private HotKeyService? _hotKeyService;
        private readonly ILogger<MainWindow> _logger;
        private readonly AppSettings _appSettings;

        public MainWindow() : this(new AppSettings())
        {
        }

        public MainWindow(AppSettings appSettings)
        {
            InitializeComponent();

            _appSettings = appSettings;

            // Create a temporary logger until DI logger is available
            _logger = new NullLogger<MainWindow>();

            // Initialize hotkey after window handle is available
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Register global hotkeys from configuration
            RegisterGlobalHotKeys();
        }

        private void RegisterGlobalHotKeys()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                _hotKeyService = new HotKeyService(helper.Handle);

                // Get hotkey configuration
                var modifiers = _appSettings.Application.HotKey.GetModifierFlags();
                var key = _appSettings.Application.HotKey.GetVirtualKeyCode();

                // Register hotkey from configuration
                _hotKeyService.RegisterHotKey(modifiers, key, () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (WindowState == WindowState.Minimized)
                        {
                            WindowState = WindowState.Normal;
                        }

                        Activate();

                        // Start recording if not already active
                        var vm = (MainViewModel?)DataContext;
                        if (vm is not { IsRecording: false }) return;

                        vm.StartRecordingCommand?.Execute(null);
                        _logger.LogInformation("Recording started via hotkey {Modifiers}+{Key}", 
                            _appSettings.Application.HotKey.Modifiers, 
                            _appSettings.Application.HotKey.Key);
                    });
                });

                var hotkeyDisplay = $"{_appSettings.Application.HotKey.Modifiers}+{_appSettings.Application.HotKey.Key}";
                Title += $" [Hotkey: {hotkeyDisplay} enabled]";
                _logger.LogInformation("Hotkey {HotKey} registered successfully", hotkeyDisplay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering hotkey: {Message}", ex.Message);
                MessageBox.Show($"Error registering hotkey: {ex.Message}", "Error");
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                hwndSource?.AddHook(WndProc);
            }
            catch
            {
                /* Silently ignore if hooking fails */
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                _hotKeyService?.HandleHotKeyMessage(id);
                handled = true;
            }

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotKeyService?.Dispose();
            _logger.LogInformation("MainWindow closed");
            base.OnClosed(e);
        }
    }
}