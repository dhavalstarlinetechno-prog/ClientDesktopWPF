namespace ClientDesktop.Core.Models
{
    public class TickData
    {
        public string SymbolName { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Ltp { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Open { get; set; }
        public double PreviousClose { get; set; }
        public long UpdateTime { get; set; }
    }
}
