using Microsoft.Extensions.Logging;
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
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint VK_R = 0x52; // R key virtual code

        public MainWindow()
        {
            InitializeComponent();

            // Create a temporary logger until DI logger is available
            _logger = new NullLogger<MainWindow>();

            // Initialize hotkey after window handle is available
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Hotkey: Ctrl+Alt+R to start recording
            RegisterGlobalHotKeys();
        }

        private void RegisterGlobalHotKeys()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                _hotKeyService = new HotKeyService(helper.Handle);

                // Register hotkey: Ctrl+Alt+R
                _hotKeyService.RegisterHotKey(MOD_CTRL | MOD_ALT, VK_R, () =>
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
                        _logger.LogInformation("Recording started via hotkey Ctrl+Alt+R");
                    });
                });

                Title += " [Hotkey: Ctrl+Alt+R enabled]";
                _logger.LogInformation("Hotkey Ctrl+Alt+R registered successfully");
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