using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Колонки хвоста таблицы оценок: «часы», фиксированный зазор, «оценка» (доля share применяется к ширине без зазора).</summary>
public sealed class AppendixGradesTailColumnWidthMultiConverter : IMultiValueConverter
{
    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 3 ||
            values.Any(static v => v == DependencyProperty.UnsetValue || v == null))
            return DependencyProperty.UnsetValue;

        var total = ToNonNegativeDouble(values[0]);
        var share = ToShare(values[1]);
        var gapRaw = ToNonNegativeDouble(values[2]);
        var which = parameter?.ToString() ?? "";

        if (total <= 0)
            return new GridLength(1, GridUnitType.Star);

        var w0 = GradesSubjectColWidth(total);
        var r = Math.Max(0, total - w0);
        var gap = ClampGap(gapRaw, r);
        var r2 = Math.Max(0, r - gap);
        var wHours = Math.Max(0, Math.Floor(r2 * share));
        var wGrade = Math.Max(0, r2 - wHours);

        return which switch
        {
            "hours" => new GridLength(wHours),
            "gap" => new GridLength(gap),
            "grade" => new GridLength(wGrade),
            _ => new GridLength(1, GridUnitType.Star),
        };
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public static double GradesSubjectColWidth(double total)
    {
        const double a = 280, b = 70, c = 125;
        return Math.Max(0, Math.Floor(total * a / (a + b + c)));
    }

    /// <summary>Максимально допустимый зазор при данной ширине хвоста <paramref name="tailWidth"/>.</summary>
    public static double MaxGapForTail(double tailWidth)
    {
        const double minHours = 22;
        const double minGrade = 32;
        return Math.Max(0, tailWidth - minHours - minGrade);
    }

    public static double ClampGap(double gap, double tailWidth) =>
        Math.Clamp(gap, 0, MaxGapForTail(tailWidth));

    private static double ToNonNegativeDouble(object? value)
    {
        if (value == null || value == DependencyProperty.UnsetValue)
            return 0;
        var d = value is double dd ? dd : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (double.IsNaN(d) || double.IsInfinity(d))
            return 0;
        return Math.Max(0, d);
    }

    private static double ToShare(object? value)
    {
        var s = value is double dd ? dd : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (double.IsNaN(s) || double.IsInfinity(s))
            return DefaultShare;
        return Math.Clamp(s, 0.08, 0.92);
    }

    public const double DefaultShare = 70.0 / (70 + 125);

    /// <summary>Ширины колонок хвоста таблицы оценок (предмет | часы | зазор | оценка), в px холста приложения.</summary>
    public static (double wSubject, double wHours, double wGap, double wGrade) ComputeTailColumnWidths(
        double tableWidth, double share, double gapPx)
    {
        var total = Math.Max(0, tableWidth);
        var w0 = GradesSubjectColWidth(total);
        var r = Math.Max(0, total - w0);
        var gap = ClampGap(gapPx, r);
        var r2 = Math.Max(0, r - gap);
        var sh = Math.Clamp(share, 0.08, 0.92);
        var wHours = Math.Max(0, Math.Floor(r2 * sh));
        var wGrade = Math.Max(0, r2 - wHours);
        return (w0, wHours, gap, wGrade);
    }
}
