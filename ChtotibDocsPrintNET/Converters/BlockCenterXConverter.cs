using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Ширина блока → X = ширина/2 + delta (ConverterParameter, число со знаком).</summary>
public sealed class BlockCenterXConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || value == DependencyProperty.UnsetValue)
            return 0.0;

        double w;
        try
        {
            w = value is double d ? d : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return 0.0;
        }

        if (double.IsNaN(w) || double.IsInfinity(w) || w < 0)
            return 0.0;

        var delta = 0.0;
        if (parameter is string s && s.Length > 0 && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
            delta = p;
        return w * 0.5 + delta;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
