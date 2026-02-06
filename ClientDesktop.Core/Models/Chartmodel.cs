namespace ClientDesktop.Core.Models
{
    public class Chartmodel
    {
        public string SymbolName { get; set; }
        public DateTime Time { get; set; }
        public long UpdateTime { get; set; }
        public decimal OpenBid { get; set; }
        public decimal OpenAsk { get; set; }
        public decimal OpenLtp { get; set; }
        public decimal HighBid { get; set; }
        public decimal HighAsk { get; set; }
        public decimal HighLtp { get; set; }
        public decimal LowBid { get; set; }
        public decimal LowAsk { get; set; }
        public decimal LowLtp { get; set; }
        public decimal CloseBid { get; set; }
        public decimal CloseAsk { get; set; }
        public decimal CloseLtp { get; set; }
        public decimal Volume { get; set; }
    }
}
