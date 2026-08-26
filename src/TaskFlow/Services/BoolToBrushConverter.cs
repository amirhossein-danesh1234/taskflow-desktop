using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DoktorTasks;

public class BoolToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBrush = new(System.Windows.Media.Color.FromRgb(56, 178, 172));
    private static readonly SolidColorBrush InactiveBrush = new(System.Windows.Media.Color.FromRgb(90, 100, 120));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return ActiveBrush;
        return InactiveBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
