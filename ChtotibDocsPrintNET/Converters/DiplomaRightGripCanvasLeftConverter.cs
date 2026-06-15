using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

/// <summary>Canvas.Left правой вертикальной ручки ширины блока для холста диплома (полоса ~6 px).</summary>
public class DiplomaRightGripCanvasLeftConverter : IMultiValueConverter
{
    public const double GripWidth = 6;

    public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2 ||
            values.Any(static v => v == DependencyProperty.UnsetValue || v == null))
            return DependencyProperty.UnsetValue;

        var left = values[0] is double dl ? dl : System.Convert.ToDouble(values[0], CultureInfo.InvariantCulture);
        var w = values[1] is double dw ? dw : System.Convert.ToDouble(values[1], CultureInfo.InvariantCulture);
        return left + w - GripWidth;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
