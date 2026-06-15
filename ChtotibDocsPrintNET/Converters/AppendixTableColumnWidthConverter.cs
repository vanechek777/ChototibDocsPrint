using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>
/// Ширины колонок строк таблицы на предпросмотре приложения в пикселях с округлением вниз и отдачей остатка последней колонке,
/// чтобы сумма колонок в точности равнялась ширине ItemsControl (без «дрейфа» звёздочной сетки WPF).
/// </summary>
public sealed class AppendixTableColumnWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var total = ToNonNegativeDouble(value);
        if (total <= 0)
            return new GridLength(1, GridUnitType.Star);

        var key = parameter?.ToString() ?? "";
        return key switch
        {
            "g0" => new GridLength(AppendixGradesTailColumnWidthMultiConverter.GradesSubjectColWidth(total)),
            "c0" => new GridLength(CourseworkCol0(total)),
            "c1" => new GridLength(CourseworkCol1(total)),
            _ => new GridLength(1, GridUnitType.Star),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static double ToNonNegativeDouble(object? value)
    {
        if (value == null || value == DependencyProperty.UnsetValue)
            return 0;
        var d = value is double dd ? dd : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (double.IsNaN(d) || double.IsInfinity(d))
            return 0;
        return Math.Max(0, d);
    }

    /// <summary>Ширина колонки «предмет» в таблице курсовых (px, холст приложения).</summary>
    public static double CourseworkSubjectColumnWidth(double total) => CourseworkCol0(Math.Max(0, total));

    /// <summary>Ширина колонки «оценка» в таблице курсовых (px).</summary>
    public static double CourseworkGradeColumnWidth(double total) => CourseworkCol1(Math.Max(0, total));

    // Лицевая курсовые: ~280 + 100 при ширине списка 380.
    private static double CourseworkCol0(double total)
    {
        const double a = 280, b = 100;
        return Math.Max(0, Math.Floor(total * a / (a + b)));
    }

    private static double CourseworkCol1(double total)
    {
        var w0 = CourseworkCol0(total);
        return Math.Max(0, total - w0);
    }

}
