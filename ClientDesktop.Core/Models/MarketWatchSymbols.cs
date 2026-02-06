using System.ComponentModel;

namespace ClientDesktop.Core.Models
{
    public class MarketWatchSymbols : INotifyPropertyChanged
    {
        private decimal _bid, _ask, _ltp, _high, _low, _buyVolume, _sellVolume, _open, _previousClose;
        private decimal _spread, _dcp, _dcv;
        private long _updateTime;

        [Browsable(false)]
        public int SymbolId { get; set; }

        [Browsable(false)]
        public int symbolDigit { get; set; }

        [DisplayName("Symbol")]
        public string SymbolName { get; set; }

        public decimal Bid { get => _bid; set { _bid = value; OnPropertyChanged(nameof(Bid)); } }
        public decimal Ask { get => _ask; set { _ask = value; OnPropertyChanged(nameof(Ask)); } }
        public decimal LTP { get => _ltp; set { _ltp = value; OnPropertyChanged(nameof(LTP)); } }
        public decimal High { get => _high; set { _high = value; OnPropertyChanged(nameof(High)); } }
        public decimal Low { get => _low; set { _low = value; OnPropertyChanged(nameof(Low)); } }
        public decimal Open { get => _open; set { _open = value; OnPropertyChanged(nameof(Open)); } }

        [DisplayName("Close")]
        public decimal PreviousClose { get => _previousClose; set { _previousClose = value; OnPropertyChanged(nameof(PreviousClose)); } }

        [DisplayName("Spread")]
        public decimal Spread { get => _spread; set { _spread = value; OnPropertyChanged(nameof(Spread)); } }

        [DisplayName("DCP")]
        public decimal DailyChangePercent { get => _dcp; set { _dcp = value; OnPropertyChanged(nameof(DailyChangePercent)); } }

        [DisplayName("DCV")]
        public decimal DailyChangeValue { get => _dcv; set { _dcv = value; OnPropertyChanged(nameof(DailyChangeValue)); } }

        [Browsable(false)]
        public long UpdateTime { get => _updateTime; set { _updateTime = value; OnPropertyChanged(nameof(UpdateDateTime)); } }

        [DisplayName("Time")]
        public DateTime UpdateDateTime => UpdateTime > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).LocalDateTime
            : DateTime.MinValue;

        [Browsable(false)]
        public decimal BuyVolume { get => _buyVolume; set { _buyVolume = value; OnPropertyChanged(nameof(BuyVolume)); } }

        [Browsable(false)]
        public decimal SellVolume { get => _sellVolume; set { _sellVolume = value; OnPropertyChanged(nameof(SellVolume)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MarketWatchApiResponse
    {
        public MarketWatchData data { get; set; }
        public object exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class MarketWatchData
    {
        public string clientId { get; set; }
        public int fontSize { get; set; }
        public bool clientProfileUpdated { get; set; }
        public object displayColumnNames { get; set; }
        public List<MarketWatchApiSymbol> symbols { get; set; }
    }

    public class MarketWatchApiSymbol
    {
        public int symbolId { get; set; }
        public string symbolName { get; set; }
        public int securityId { get; set; }
        public string masterSymbolName { get; set; }
        public int symbolDigits { get; set; }
        public string spreadType { get; set; }
        public decimal spreadValue { get; set; }
        public decimal spreadBalance { get; set; }
        public bool symbolStatus { get; set; }
        public bool symbolHide { get; set; }
        public int displayPosition { get; set; }
        public SymbolBook symbolBook { get; set; }
    }

    public class SymbolBook
    {
        public string symbolName { get; set; }
        public decimal bid { get; set; }
        public decimal ask { get; set; }
        public decimal ltp { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
        public long updateTime { get; set; }
        public decimal buyVolume { get; set; }
        public decimal sellVolume { get; set; }
        public decimal open { get; set; }
        public decimal previousClose { get; set; }
    }
}