using System;
using System.Globalization;
using System.Windows.Data;

namespace SpeechAdmin.Converters
{
    public class RecordingStatusConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool and true ? "🔴 Aufnahme läuft" : "Bereit";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}