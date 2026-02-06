namespace ClientDesktop.Core.Models
{
    public  class TradeRequestModel
    {
        public class TradeRequest
        {
            public string Username { get; set; }
            public int SymbolId { get; set; }
            public PlaceInstruction PlaceInstruction { get; set; }
            public MarketInfo MarketInfo { get; set; }
            public DeviceDetail DeviceDetail { get; set; }
            public string OrderFulfillment { get; set; }
            public string Comment { get; set; }
        }

        public class PlaceInstruction
        {
            public string OrderType { get; set; }     // e.g. "Market"
            public string Side { get; set; }          // e.g. "ASK"
            public LimitMarketOrder LimitMarketOrder { get; set; }
        }

        public class LimitMarketOrder
        {
            public double Price { get; set; }
            public double Volume { get; set; }
            public double CurrentPrice { get; set; }
        }

        public class MarketInfo
        {
            public DateTime? SymbolExpiry { get; set; }
        }

        public class DeviceDetail
        {
            public string ClientIP { get; set; }
            public string Device { get; set; }
            public string Reason { get; set; }
        }
    }
}
