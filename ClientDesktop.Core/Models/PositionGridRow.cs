using ClientDesktop.Core.Base;
using System;

namespace ClientDesktop.Core.Models
{
    public enum RowType { Position, Footer, Order }

    public class PositionGridRow : ViewModelBase
    {
        // =========================
        // BACKING FIELDS
        // =========================

        private double? _currentPrice;
        private double? _volume;
        private string _currentPriceDisplay;
        private decimal? _pnl;
        private string _averagePriceDisplay;
        private string _symbolName;
        private string _priceColor = "Black";
        private string _pnlColor = "Black";
        private DateTime? _symbolExpiry;           // ? ADD 1: backing field

        // =========================
        // BASIC PROPERTIES
        // =========================

        public string Id { get; set; }
        public int SymbolId { get; set; }
        public int SymbolDigit { get; set; }

        private DateTime? _time;
        public string Side { get; set; }
        public string OrderType { get; set; }
        public string Comment { get; set; }
        public RowType Type { get; set; }

        // =========================
        // NOTIFY PROPERTIES
        // =========================

        public string SymbolName
        {
            get => _symbolName;
            set { if (_symbolName != value) { _symbolName = value; OnPropertyChanged(); } }
        }

        public DateTime? Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(); }
        }

        public double? Volume
        {
            get => _volume;
            set { if (_volume != value) { _volume = value; OnPropertyChanged(); } }
        }

        public double? AveragePrice { get; set; }

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
            set
            {
                if (_pnl != value)
                {
                    _pnl = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PnlDisplay)); // ? ADD 2: refresh computed on live tick
                }
            }
        }

        /// <summary>
        /// Raw expiry string from API (ISO format: yyyy-MM-ddTHH:mm:ss).
        /// Null or empty means GTC (no expiry).
        /// </summary>
        public DateTime? SymbolExpiry                       // ? ADD 3: new notifiable property
        {
            get => _symbolExpiry;
            set
            {
                if (_symbolExpiry != value)
                {
                    _symbolExpiry = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PnlDisplay)); // refresh computed when expiry set
                }
            }
        }

        /// <summary>
        /// What the Floating P/L column displays:
        /// - Order row with expiry  ? expiry date formatted as dd/MM/yy HH:mm
        /// - Order row GTC          ? empty string
        /// - Position row           ? Pnl value formatted as F2
        /// - Footer row             ? not used (footer has its own panel)
        /// </summary>
        public string PnlDisplay
        {
            get
            {
                if (IsOrder)
                    return SymbolExpiry.HasValue
                        ? SymbolExpiry.Value.ToString("dd/MM/yy HH:mm", System.Globalization.CultureInfo.InvariantCulture)
                        : string.Empty;

                return Pnl.HasValue ? Pnl.Value.ToString("F2") : string.Empty;
            }
        }

        // =========================
        // COLOR PROPERTIES
        // =========================

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

        // =========================
        // HELPER FLAGS
        // =========================

        public bool IsFooter => Type == RowType.Footer;
        public bool IsOrder => Type == RowType.Order;
        public bool IsPosition => Type == RowType.Position;
    }
}