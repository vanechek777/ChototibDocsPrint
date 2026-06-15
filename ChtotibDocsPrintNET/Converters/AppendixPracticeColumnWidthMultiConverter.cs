using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Ширины пяти зон таблицы практик: виды | зазор | средства | зазор | место.</summary>
public sealed class AppendixPracticeColumnWidthMultiConverter : IMultiValueConverter
{
    public const double DefaultActivityShare = 0.18;
    public const double DefaultMeansShareOfRemainder = 0.50 / (0.50 + 0.32);

    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 4 ||
            values.Any(static v => v == DependencyProperty.UnsetValue || v == null))
            return DependencyProperty.UnsetValue;

        var total = ToNonNegativeDouble(values[0]);
        if (total <= 0)
            return new GridLength(1, GridUnitType.Star);

        var activityShare = ClampActivityShare(values[1]);
        var meansShare = ClampMeansShare(values[2]);
        var gapPx = ToNonNegativeDouble(values[3]);
        var (w0, gap, w1, w2) = ComputeColumnWidths(total, activityShare, meansShare, gapPx);

        return (parameter?.ToString() ?? "") switch
        {
            "p0" => new GridLength(w0),
            "gap" => new GridLength(gap),
            "p1" => new GridLength(w1),
            "p2" => new GridLength(w2),
            _ => new GridLength(1, GridUnitType.Star),
        };
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public static (double w0, double gap, double w1, double w2) ComputeColumnWidths(
        double totalWidth, double activityShare, double meansShareOfRemainder, double columnGapPx = 0)
    {
        var total = Math.Max(0, totalWidth);
        var gap = ClampGap(columnGapPx, total);
        var workable = Math.Max(0, total - gap * 2);
        var (w0, w1, w2) = ComputeContentWidths(workable, activityShare, meansShareOfRemainder);
        return (w0, gap, w1, w2);
    }

    private static (double w0, double w1, double w2) ComputeContentWidths(
        double workableWidth, double activityShare, double meansShareOfRemainder)
    {
        var total = Math.Max(0, workableWidth);
        var sh0 = ClampActivityShare(activityShare);
        var w0 = Math.Floor(total * sh0);
        var remainder = Math.Max(0, total - w0);
        var sh1 = ClampMeansShare(meansShareOfRemainder);
        var w1 = Math.Floor(remainder * sh1);
        var w2 = Math.Max(0, remainder - w1);
        return (w0, w1, w2);
    }

    public static double MaxGapForTable(double tableWidth)
    {
        const double minCol = 18;
        return Math.Max(0, tableWidth - minCol * 3);
    }

    public static double ClampGap(double gap, double tableWidth) =>
        Math.Clamp(gap, 0, MaxGapForTable(tableWidth) * 0.5);

    public static double ClampActivityShare(object? value)
    {
        var s = ToDouble(value);
        if (double.IsNaN(s) || double.IsInfinity(s))
            return DefaultActivityShare;
        return Math.Clamp(s, 0.08, 0.45);
    }

    public static double ClampMeansShare(object? value)
    {
        var s = ToDouble(value);
        if (double.IsNaN(s) || double.IsInfinity(s))
            return DefaultMeansShareOfRemainder;
        return Math.Clamp(s, 0.15, 0.85);
    }

    private static double ToNonNegativeDouble(object? value)
    {
        var d = ToDouble(value);
        return double.IsNaN(d) || double.IsInfinity(d) ? 0 : Math.Max(0, d);
    }

    private static double ToDouble(object? value)
    {
        if (value == null || value == DependencyProperty.UnsetValue)
            return 0;
        return value is double dd ? dd : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }
}
