using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClientDesktop.View.Converters
{
    internal class Converter
    {
    }

    public class ProfitToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { if (value is string s) return s.Contains("+"); return null; }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PriceChangeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { if (value is int change) return change > 0 ? Brushes.Blue : Brushes.Red; return Brushes.Black; }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
