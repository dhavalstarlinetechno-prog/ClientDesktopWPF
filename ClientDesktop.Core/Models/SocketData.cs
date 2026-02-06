namespace ClientDesktop.Core.Models
{
    public class SocketData
    {
        // Existing
        public List<Position> Positions { get; set; } = new List<Position>();
        public List<OrderModel> PendingOrders { get; set; } = new List<OrderModel>();
        public List<Skt_Symbol> Symbols { get; set; } = new List<Skt_Symbol>(); // Now actively used for Watchlist
        public Skt_ClientBalance Balance { get; set; } = new Skt_ClientBalance();

        // NEW: From Socket Code (History & Bans)
        public List<Skt_Deal> HistoryDeals { get; set; } = new List<Skt_Deal>();
        public List<Skt_BanScript> BanScripts { get; set; } = new List<Skt_BanScript>();
    }

    public class Skt_Position
    {
        public string id { get; set; }
        public int symbolId { get; set; }
        public string symbolName { get; set; }
        public decimal totalVolume { get; set; }
        public decimal averagePrice { get; set; }
        public decimal currentPrice { get; set; }
        public decimal pnl { get; set; }
        public string orderType { get; set; }
        public string orderId { get; set; }
    }

    public class Skt_PendingOrder
    {
        public string orderId { get; set; }
        public int symbolId { get; set; }
        public string symbolName { get; set; }
        public decimal volume { get; set; }
        public decimal limitPrice { get; set; }
        public string orderFulfillment { get; set; }
        public string orderType { get; set; }
    }

    public class Skt_Symbol
    {
        public int symbolId { get; set; }
        public string symbolName { get; set; }
        public string masterSymbolName { get; set; } // Added for Ban Script logic
        public int securityId { get; set; } // Added for Security updates
        public decimal bid { get; set; }
        public decimal ask { get; set; }
        public decimal spreadBalance { get; set; }
        public bool symbolStatus { get; set; }
        public bool securityStatus { get; set; } // Added from Socket events
    }

    public class Skt_ClientBalance
    {
        public decimal balance { get; set; }
        public decimal creditAmount { get; set; }
        public decimal pnl { get; set; }
        public decimal freeMarginAmount { get; set; }
        public decimal occupiedMarginAmount { get; set; }
        public decimal floatingPLAmount { get; set; }
        public decimal uplineAmount { get; set; } // Added from socket logic
        public decimal uplineCommission { get; set; } // Added from socket logic
    }

    public class Skt_Deal
    {
        public string orderId { get; set; }
        public string symbolName { get; set; }
        public decimal volume { get; set; }
        public decimal pnl { get; set; }
        public string type { get; set; }
        public DateTime closeTime { get; set; }
    }

    public class Skt_BanScript
    {
        public string masterSymbolName { get; set; }
        public string reason { get; set; }
    }

}
