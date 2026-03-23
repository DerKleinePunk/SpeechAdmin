using System.Collections.Generic;
using System.Linq;

namespace SpeechAdmin.Configuration
{
    /// <summary>
    /// Root configuration class for application settings
    /// </summary>
    public class AppSettings
    {
        public ApplicationSettings Application { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    /// <summary>
    /// Application-specific settings
    /// </summary>
    public class ApplicationSettings
    {
        public string Name { get; set; } = "SpeechAdmin";
        public HotKeySettings HotKey { get; set; } = new();
    }

    /// <summary>
    /// Hotkey configuration
    /// </summary>
    public class HotKeySettings
    {
        public string Modifiers { get; set; } = "Ctrl+Alt";
        public string Key { get; set; } = "R";

        /// <summary>
        /// Parses modifiers string and returns combined modifier flags
        /// </summary>
        public uint GetModifierFlags()
        {
            var parts = Modifiers.Split('+');
            return parts.Select(part => part.Trim())
                .Aggregate<string, uint>(0, (current, trimmed) => current | (uint)(trimmed switch
                {
                    "Ctrl" => 0x0002, // MOD_CTRL
                    "Alt" => 0x0001, // MOD_ALT
                    "Shift" => 0x0004, // MOD_SHIFT
                    "Win" => 0x0008, // MOD_WIN
                    _ => 0
                }));
        }

        /// <summary>
        /// Gets the virtual key code for the configured key
        /// </summary>
        public uint GetVirtualKeyCode()
        {
            if (string.IsNullOrEmpty(Key) || Key.Length == 0)
                return 0;

            // Convert single character to uppercase and get virtual key code
            var keyChar = char.ToUpper(Key[0]);
            return keyChar;
        }
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LoggingSettings
    {
        public Dictionary<string, string> LogLevel { get; set; } = new()
        {
            { "Default", "Information" }
        };

        public FileLoggingSettings File { get; set; } = new();
    }

    /// <summary>
    /// File logging settings
    /// </summary>
    public class FileLoggingSettings
    {
        public bool Enabled { get; set; } = true;
        public string Path { get; set; } = "logs/speechadmin-.log";
        public string RollingInterval { get; set; } = "Day";
        public int? RetainedFileCountLimit { get; set; } = 7;
    }
}