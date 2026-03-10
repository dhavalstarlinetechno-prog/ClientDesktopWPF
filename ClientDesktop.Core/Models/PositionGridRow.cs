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
        private double? _currentPrice;
        private string _currentPriceDisplay; // Naya property UI format ke liye
        private decimal? _pnl;

        // --- COLOUR LOGIC PROPERTIES ---
        private string _priceColor = "Black";
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

        private string _pnlColor = "Black";
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
        // -------------------------------

        public string Id { get; set; }
        public int SymbolId { get; set; }
        public int SymbolDigit { get; set; }
        private string _symbolName;
        public string SymbolName
        {
            get => _symbolName;
            set
            {
                _symbolName = value;
                OnPropertyChanged(); 
            }
        }
        public DateTime? Time { get; set; }
        public string Side { get; set; }
        public string OrderType { get; set; }

        public double? Volume { get; set; }
        public double? AveragePrice { get; set; }

        // Nayi property UI formatting ke liye
        private string _averagePriceDisplay;
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

        // Formatted Text (Sirf UI mein exactly F2/F3/F5 dikhane ke liye)
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

        public string Comment { get; set; }

        public RowType Type { get; set; }

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