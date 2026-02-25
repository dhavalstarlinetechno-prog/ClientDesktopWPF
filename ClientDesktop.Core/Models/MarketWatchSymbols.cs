using ClientDesktop.Core.Base;
using System.ComponentModel;

namespace ClientDesktop.Core.Models
{
    public class MarketWatchSymbols : ViewModelBase
    {
        private double _bid;
        private int _bidDir;
        private double _ask;
        private int _askDir;
        private double _ltp;
        private int _ltpDir;
        private double _high;
        private double _low;
        private double _open;
        private double _close;
        private double _spread;
        private string _dcp;
        private double _dcv;
        private string _time;
        private int _timeDir;

        public int SymbolId { get; set; }
        public int SymbolDigit { get; set; }
        public string SymbolName { get; set; }
        public bool IsUp { get; set; }

        public double Bid { get => _bid; set { if (SetProperty(ref _bid, value)) OnPropertyChanged(nameof(DisplayBid)); } }
        public int BidDir { get => _bidDir; set => SetProperty(ref _bidDir, value); }
        public string DisplayBid => _bid.ToString($"F{SymbolDigit}");

        public double Ask { get => _ask; set { if (SetProperty(ref _ask, value)) OnPropertyChanged(nameof(DisplayAsk)); } }
        public int AskDir { get => _askDir; set => SetProperty(ref _askDir, value); }
        public string DisplayAsk => _ask.ToString($"F{SymbolDigit}");

        public double Ltp { get => _ltp; set { if (SetProperty(ref _ltp, value)) OnPropertyChanged(nameof(DisplayLtp)); } }
        public int LtpDir { get => _ltpDir; set => SetProperty(ref _ltpDir, value); }
        public string DisplayLtp => _ltp.ToString($"F{SymbolDigit}");

        public double High { get => _high; set { if (SetProperty(ref _high, value)) OnPropertyChanged(nameof(DisplayHigh)); } }
        public string DisplayHigh => _high.ToString($"F{SymbolDigit}");

        public double Low { get => _low; set { if (SetProperty(ref _low, value)) OnPropertyChanged(nameof(DisplayLow)); } }
        public string DisplayLow => _low.ToString($"F{SymbolDigit}");

        public double Open { get => _open; set { if (SetProperty(ref _open, value)) OnPropertyChanged(nameof(DisplayOpen)); } }
        public string DisplayOpen => _open.ToString($"F{SymbolDigit}");

        public double Close { get => _close; set { if (SetProperty(ref _close, value)) OnPropertyChanged(nameof(DisplayClose)); } }
        public string DisplayClose => _close.ToString($"F{SymbolDigit}");

        public double Spread { get => _spread; set { if (SetProperty(ref _spread, value)) OnPropertyChanged(nameof(DisplaySpread)); } }
        public string DisplaySpread => _spread.ToString($"F{SymbolDigit}");

        public string Dcp { get => _dcp; set => SetProperty(ref _dcp, value); }

        public double Dcv { get => _dcv; set { if (SetProperty(ref _dcv, value)) OnPropertyChanged(nameof(DisplayDcv)); } }
        public string DisplayDcv => _dcv.ToString($"F{SymbolDigit}");

        public string Time { get => _time; set => SetProperty(ref _time, value); }
        public int TimeDir { get => _timeDir; set => SetProperty(ref _timeDir, value); }
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