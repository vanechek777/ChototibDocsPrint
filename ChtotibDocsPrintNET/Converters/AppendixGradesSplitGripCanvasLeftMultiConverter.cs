using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Canvas.Left ручки зазора между «часы» и «оценка» (центр в колонке зазора).</summary>
public sealed class AppendixGradesSplitGripCanvasLeftMultiConverter : IMultiValueConverter
{
    public const double GripWidth = 8;

    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 4 ||
            values.Any(static v => v == DependencyProperty.UnsetValue || v == null))
            return DependencyProperty.UnsetValue;

        var left = ToDouble(values[0]);
        var total = Math.Max(0, ToDouble(values[1]));
        var share = ToShare(values[2]);
        var gapRaw = ToDouble(values[3]);

        var w0 = AppendixGradesTailColumnWidthMultiConverter.GradesSubjectColWidth(total);
        var r = Math.Max(0, total - w0);
        var gap = AppendixGradesTailColumnWidthMultiConverter.ClampGap(gapRaw, r);
        var r2 = Math.Max(0, r - gap);
        var wHours = Math.Max(0, Math.Floor(r2 * share));
        return left + w0 + wHours + gap * 0.5 - GripWidth * 0.5;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static double ToDouble(object? value)
    {
        if (value == null || value == DependencyProperty.UnsetValue)
            return 0;
        var d = value is double dd ? dd : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return double.IsNaN(d) || double.IsInfinity(d) ? 0 : d;
    }

    private static double ToShare(object? value)
    {
        var s = value is double dd ? dd : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (double.IsNaN(s) || double.IsInfinity(s))
            return AppendixGradesTailColumnWidthMultiConverter.DefaultShare;
        return Math.Clamp(s, 0.08, 0.92);
    }
}
