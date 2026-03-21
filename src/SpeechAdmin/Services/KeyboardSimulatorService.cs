using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Service for keyboard input simulation via Windows API
    /// </summary>
    public class KeyboardSimulatorService(ILogger<KeyboardSimulatorService>? logger = null)
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private readonly ILogger<KeyboardSimulatorService> _logger = logger ?? new NullLogger<KeyboardSimulatorService>();

        /// <summary>
        /// Types text via clipboard and Ctrl+V
        /// </summary>
        public void TypeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                // Small delay to ensure target app is focused
                System.Threading.Thread.Sleep(100);

                // Copy text to clipboard
                System.Windows.Forms.Clipboard.SetText(text);

                // Wait briefly
                System.Threading.Thread.Sleep(50);

                // Simulate Ctrl+V to paste
                PressKey(VirtualKeyCode.ControlKey);
                PressKey(VirtualKeyCode.V);
                ReleaseKey(VirtualKeyCode.V);
                ReleaseKey(VirtualKeyCode.ControlKey);

                _logger.LogDebug("Text inserted ({CharacterCount} characters)", text.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating keyboard: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Simulates key press
        /// </summary>
        private void PressKey(VirtualKeyCode keyCode)
        {
            keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Simulates key release
        /// </summary>
        private void ReleaseKey(VirtualKeyCode keyCode)
        {
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        /// <summary>
        /// Virtual key codes for Windows
        /// </summary>
        private enum VirtualKeyCode
        {
            ControlKey = 0x11,
            V = 0x56,
            Return = 0x0D,
            Space = 0x20
        }
    }
}