using System;
using Microsoft.Extensions.Logging;

namespace SpeechAdmin.SpeechModels
{
    public partial class WhisperModel
    {
        [LoggerMessage(LogLevel.Debug, "   [{Start} --> {End}]: {Text}")]
        partial void LogStartEndText(TimeSpan Start, TimeSpan End, string Text);
    }
}