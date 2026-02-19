using System.ComponentModel;

namespace ClientDesktop.Core.Models
{
    public class MarketItem
    {
        public int SymbolId { get; set; }
        public int SymbolDigit { get; set; }
        public string SymbolName { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Ltp { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double Spread { get; set; }
        public string Dcp { get; set; }
        public double Dcv { get; set; }
        public string Time { get; set; }
        public bool IsUp { get; set; } // For the small arrow indicator
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