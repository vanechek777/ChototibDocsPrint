using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Canvas.Left нижней горизонтальной ручки высоты (полоса ~28 px по центру блока).</summary>
public sealed class AppendixBottomGripCanvasLeftConverter : IMultiValueConverter
{
    public const double GripWidth = 28;

    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2 ||
            values.Any(static v => v == DependencyProperty.UnsetValue || v == null))
            return DependencyProperty.UnsetValue;

        var left = values[0] is double dl ? dl : System.Convert.ToDouble(values[0], CultureInfo.InvariantCulture);
        var w = values[1] is double dw ? dw : System.Convert.ToDouble(values[1], CultureInfo.InvariantCulture);
        return left + w * 0.5 - GripWidth * 0.5;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Canvas.Top нижней ручки высоты (полоса ~6 px у нижнего края блока).</summary>
public sealed class AppendixBottomGripCanvasTopConverter : IMultiValueConverter
{
    public const double GripHeight = 6;

    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2 ||
            values.Any(static v => v == DependencyProperty.UnsetValue || v == null))
            return DependencyProperty.UnsetValue;

        var top = values[0] is double dt ? dt : System.Convert.ToDouble(values[0], CultureInfo.InvariantCulture);
        var h = values[1] is double dh ? dh : System.Convert.ToDouble(values[1], CultureInfo.InvariantCulture);
        return top + h - GripHeight;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
