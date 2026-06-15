using System;
using System.Globalization;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Уменьшает размер области предпросмотра на отступ (заголовок Expander под холстом).</summary>
public sealed class PreviewFitSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double size)
            return 100.0;

        var margin = 36.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, culture, out var m))
            margin = m;

        return Math.Max(80, size - margin);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
