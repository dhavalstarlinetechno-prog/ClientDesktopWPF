using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClientDesktop.View.Converters
{
    public class ProfitToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double profit)
            {
                if (profit > 0) return Brushes.Blue; // Profit
                if (profit < 0) return Brushes.Red;  // Loss
            }
            if (value is decimal profitDec)
            {
                if (profitDec > 0) return Brushes.Blue;
                if (profitDec < 0) return Brushes.Red;
            }
            return Brushes.Black; // 0 or Null
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OrderTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string side)
            {
                string lowerSide = side.ToLower();

                if (lowerSide.Contains("buy"))
                    return new SolidColorBrush(Color.FromRgb(0, 114, 188)); 

                if (lowerSide.Contains("sell"))
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69)); 
            }
            return Brushes.Gray; // Default fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PriceChangeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double priceChange)
            {
                if (priceChange > 0) return Brushes.Blue;
                if (priceChange < 0) return Brushes.Red;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string side)
            {
                return side.ToLower() == "bid" ? "Sell" : "Buy";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}