using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Service für Global Hotkey Management
    /// </summary>
    public class HotKeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private IntPtr _windowHandle;
        private int _hotKeyId = 1;
        private Dictionary<int, Action> _hotKeyHandlers = new();

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CTRL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public HotKeyService(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

        /// <summary>
        /// Registriert einen Hotkey
        /// </summary>
        public bool RegisterHotKey(uint modifiers, uint virtualKey, Action callback)
        {
            int id = _hotKeyId++;

            if (RegisterHotKey(_windowHandle, id, modifiers, virtualKey))
            {
                _hotKeyHandlers[id] = callback;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Behandelt Hotkey-Nachrichten
        /// </summary>
        public void HandleHotKeyMessage(int id)
        {
            if (_hotKeyHandlers.TryGetValue(id, out var handler))
            {
                handler?.Invoke();
            }
        }

        public void Dispose()
        {
            foreach (var id in _hotKeyHandlers.Keys)
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _hotKeyHandlers.Clear();
        }
    }
}
