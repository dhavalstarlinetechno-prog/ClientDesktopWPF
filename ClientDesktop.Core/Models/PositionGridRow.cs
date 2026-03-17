using ClientDesktop.Core.Base;

namespace ClientDesktop.Core.Models
{
    public enum RowType
    {
        Position,
        Footer,
        Order
    }

    public class PositionGridRow : ViewModelBase
    {
        // =========================
        // ?? BACKING FIELDS
        // =========================

        private double? _currentPrice;
        private double? _volume;
        private string _currentPriceDisplay;
        private decimal? _pnl;
        private string _averagePriceDisplay;
        private string _symbolName;

        private string _priceColor = "Black";
        private string _pnlColor = "Black";

        // =========================
        // ?? BASIC PROPERTIES
        // =========================

        public string Id { get; set; }
        public int SymbolId { get; set; }
        public int SymbolDigit { get; set; }

        public string Time { get; set; }
        public string Side { get; set; }
        public string OrderType { get; set; }
        public string Comment { get; set; }

        public RowType Type { get; set; }

        // =========================
        // ?? NOTIFY PROPERTIES
        // =========================

        public string SymbolName
        {
            get => _symbolName;
            set
            {
                if (_symbolName != value)
                {
                    _symbolName = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? AveragePrice { get; set; }

        public string AveragePriceDisplay
        {
            get => _averagePriceDisplay;
            set
            {
                if (_averagePriceDisplay != value)
                {
                    _averagePriceDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? CurrentPrice
        {
            get => _currentPrice;
            set
            {
                if (_currentPrice != value)
                {
                    _currentPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentPriceDisplay
        {
            get => _currentPriceDisplay;
            set
            {
                if (_currentPriceDisplay != value)
                {
                    _currentPriceDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal? Pnl
        {
            get => _pnl;
            set
            {
                if (_pnl != value)
                {
                    _pnl = value;
                    OnPropertyChanged();
                }
            }
        }

        // =========================
        // ?? COLOR PROPERTIES
        // =========================

        public string PriceColor
        {
            get => _priceColor;
            set
            {
                if (_priceColor != value)
                {
                    _priceColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PnlColor
        {
            get => _pnlColor;
            set
            {
                if (_pnlColor != value)
                {
                    _pnlColor = value;
                    OnPropertyChanged();
                }
            }
        }

        // =========================
        // ?? HELPER FLAGS
        // =========================

        public bool IsFooter => Type == RowType.Footer;
        public bool IsOrder => Type == RowType.Order;
        public bool IsPosition => Type == RowType.Position;
    }
}