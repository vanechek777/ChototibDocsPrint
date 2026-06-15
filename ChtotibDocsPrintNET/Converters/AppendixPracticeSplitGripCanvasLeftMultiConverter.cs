using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Canvas.Left ручки между колонками таблицы практик (0 — виды|средства, 1 — средства|место).</summary>
public sealed class AppendixPracticeSplitGripCanvasLeftMultiConverter : IMultiValueConverter
{
    public const double GripWidth = 8;

    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 5 ||
            values.Any(static v => v == DependencyProperty.UnsetValue || v == null))
            return DependencyProperty.UnsetValue;

        var left = ToDouble(values[0]);
        var total = Math.Max(0, ToDouble(values[1]));
        var activityShare = AppendixPracticeColumnWidthMultiConverter.ClampActivityShare(values[2]);
        var meansShare = AppendixPracticeColumnWidthMultiConverter.ClampMeansShare(values[3]);
        var gapPx = ToDouble(values[4]);
        var (w0, gap, w1, _) = AppendixPracticeColumnWidthMultiConverter.ComputeColumnWidths(
            total, activityShare, meansShare, gapPx);

        var which = parameter?.ToString() ?? "0";
        var split0 = left + w0 + gap;
        var split1 = left + w0 + gap + w1 + gap;
        var splitX = which switch
        {
            "1" => split1,
            "gap0" => left + w0 + gap * 0.5,
            "gap1" => left + w0 + gap + w1 + gap * 0.5,
            _ => split0,
        };
        return splitX - GripWidth * 0.5;
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
}
