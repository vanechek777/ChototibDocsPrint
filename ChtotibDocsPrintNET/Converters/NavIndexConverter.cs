using System.Globalization;
using System.Windows.Data;

namespace ChtotibDocsPrintNET.Converters;

public class NavIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string paramStr && int.TryParse(paramStr, out int paramIndex))
            return index == paramIndex;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramStr && int.TryParse(paramStr, out int paramIndex))
            return paramIndex;
        return Binding.DoNothing;
    }
}
