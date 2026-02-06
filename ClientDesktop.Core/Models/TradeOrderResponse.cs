namespace ClientDesktop.Core.Models
{
    public class TradeOrderResponse
    {
        public TradeOrderData data { get; set; }
        public object exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class TradeOrderData
    {
        public int symbolId { get; set; }
        public string symbolName { get; set; }
        public string side { get; set; }
        public double averagePrice { get; set; }
        public double currentPrice { get; set; }
        public double inVolume { get; set; }
        public string reason { get; set; }
        public string username { get; set; }
        public string id { get; set; }
        public DateTime createdAt { get; set; }
    }
}