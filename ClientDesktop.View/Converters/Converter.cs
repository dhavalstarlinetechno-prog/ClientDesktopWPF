using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ClientDesktop.Infrastructure.Helpers;

namespace ClientDesktop.View.Converters
{
    #region ProfitToColorConverter

    /// <summary>
    /// Converts a profit value to a specific color brush based on positive or negative values.
    /// </summary>
    public class ProfitToColorConverter : IValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts the numeric profit value to a color brush.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush greenBrush = new SolidColorBrush(Color.FromRgb(0, 153, 0));
            SolidColorBrush redBrush = new SolidColorBrush(Color.FromRgb(255, 59, 48));

            if (value is double profit)
            {
                if (profit > 0) return greenBrush;
                if (profit < 0) return redBrush;
            }

            if (value is decimal profitDec)
            {
                if (profitDec > 0) return greenBrush;
                if (profitDec < 0) return redBrush;
            }

            return Brushes.Black;
        }

        /// <summary>
        /// Conversion back is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

    #region OrderTypeToColorConverter

    /// <summary>
    /// Converts an order type string to a corresponding color brush.
    /// </summary>
    public class OrderTypeToColorConverter : IValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts the order side (buy/sell) to a specific SolidColorBrush.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string side)
            {
                string lowerSide = side.ToLower();

                if (lowerSide.Contains("buy"))
                {
                    return new SolidColorBrush(Color.FromRgb(0, 119, 254));
                }

                if (lowerSide.Contains("sell"))
                {
                    return new SolidColorBrush(Color.FromRgb(255, 59, 48));
                }
            }

            return Brushes.Gray;
        }

        /// <summary>
        /// Conversion back is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

    #region PriceChangeColorConverter

    /// <summary>
    /// Converts a price change value or percentage to a specific color brush.
    /// </summary>
    public class PriceChangeColorConverter : IValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts the price change to a green or red SolidColorBrush.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush greenBrush = new SolidColorBrush(Color.FromRgb(0, 153, 0));
            SolidColorBrush redBrush = new SolidColorBrush(Color.FromRgb(255, 59, 48));

            if (value is double priceChange)
            {
                if (priceChange > 0) return greenBrush;
                if (priceChange < 0) return redBrush;
            }

            if (value is string strVal && double.TryParse(strVal.Replace("%", ""), out double parsed))
            {
                if (parsed > 0) return greenBrush;
                if (parsed < 0) return redBrush;
            }

            return Brushes.Black;
        }

        /// <summary>
        /// Conversion back is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

    #region SideConverter

    /// <summary>
    /// Converts a market side string to a readable format for the UI.
    /// </summary>
    public class SideConverter : IValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts terms like bid to sell, and defaults others to buy.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string side)
            {
                return side.ToLower() == "bid" ? "Sell" : "Buy";
            }

            return value;
        }

        /// <summary>
        /// Conversion back is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

    #region StringTruncateConverter

    /// <summary>
    /// Truncates a long string and appends an ellipsis if it exceeds the maximum length.
    /// </summary>
    public class StringTruncateConverter : IValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Truncates the text to a maximum of 8 characters.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text) && text.Length > 8)
            {
                return text.Substring(0, 8) + "...";
            }

            return value;
        }

        /// <summary>
        /// Conversion back is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

    #region UtcToIstConverter

    /// <summary>
    /// Converts a UTC DateTime object to an Indian Standard Time (IST) formatted string.
    /// </summary>
    public class UtcToIstConverter : IValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts the UTC DateTime to a formatted IST string.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime utcDate)
            {
                DateTime istTime = CommonHelper.ConvertUtcToIst(utcDate);
                return istTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return value;
        }

        /// <summary>
        /// Conversion back is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion
}