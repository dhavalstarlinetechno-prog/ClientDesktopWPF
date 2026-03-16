using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClientDesktop.Core.Models
{
    public enum RowType
    {
        Position,
        Footer,
        Order
    }

    public class PositionGridRow : INotifyPropertyChanged
    {
        // Backing fields for all properties that can change dynamically
        private string _id;
        private int _symbolId;
        private int _symbolDigit;
        private string _symbolName;
        private DateTime? _time;
        private string _side;
        private string _orderType;
        private double? _volume;
        private double? _averagePrice;
        private string _averagePriceDisplay;
        private double? _currentPrice;
        private string _currentPriceDisplay;
        private decimal? _pnl;
        private string _priceColor = "Black";
        private string _pnlColor = "Black";
        private string _comment;
        private RowType _type;

        public string Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        public int SymbolId
        {
            get => _symbolId;
            set { if (_symbolId != value) { _symbolId = value; OnPropertyChanged(); } }
        }

        public int SymbolDigit
        {
            get => _symbolDigit;
            set { if (_symbolDigit != value) { _symbolDigit = value; OnPropertyChanged(); } }
        }

        public string SymbolName
        {
            get => _symbolName;
            set { if (_symbolName != value) { _symbolName = value; OnPropertyChanged(); } }
        }

        public DateTime? Time
        {
            get => _time;
            set { if (_time != value) { _time = value; OnPropertyChanged(); } }
        }

        public string Side
        {
            get => _side;
            set { if (_side != value) { _side = value; OnPropertyChanged(); } }
        }

        public string OrderType
        {
            get => _orderType;
            set { if (_orderType != value) { _orderType = value; OnPropertyChanged(); } }
        }

        public double? Volume
        {
            get => _volume;
            set { if (_volume != value) { _volume = value; OnPropertyChanged(); } }
        }

        public double? AveragePrice
        {
            get => _averagePrice;
            set { if (_averagePrice != value) { _averagePrice = value; OnPropertyChanged(); } }
        }

        public string AveragePriceDisplay
        {
            get => _averagePriceDisplay;
            set { if (_averagePriceDisplay != value) { _averagePriceDisplay = value; OnPropertyChanged(); } }
        }

        public double? CurrentPrice
        {
            get => _currentPrice;
            set { if (_currentPrice != value) { _currentPrice = value; OnPropertyChanged(); } }
        }

        public string CurrentPriceDisplay
        {
            get => _currentPriceDisplay;
            set { if (_currentPriceDisplay != value) { _currentPriceDisplay = value; OnPropertyChanged(); } }
        }

        public decimal? Pnl
        {
            get => _pnl;
            set { if (_pnl != value) { _pnl = value; OnPropertyChanged(); } }
        }

        public string PriceColor
        {
            get => _priceColor;
            set { if (_priceColor != value) { _priceColor = value; OnPropertyChanged(); } }
        }

        public string PnlColor
        {
            get => _pnlColor;
            set { if (_pnlColor != value) { _pnlColor = value; OnPropertyChanged(); } }
        }

        public string Comment
        {
            get => _comment;
            set { if (_comment != value) { _comment = value; OnPropertyChanged(); } }
        }

        public RowType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                    // In properties pe bhi depend karta hai, toh inko bhi notify kara do
                    OnPropertyChanged(nameof(IsFooter));
                    OnPropertyChanged(nameof(IsOrder));
                    OnPropertyChanged(nameof(IsPosition));
                }
            }
        }

        public bool IsFooter => Type == RowType.Footer;
        public bool IsOrder => Type == RowType.Order;
        public bool IsPosition => Type == RowType.Position;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}