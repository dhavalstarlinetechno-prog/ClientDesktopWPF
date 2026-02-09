namespace ClientDesktop.Core.Models
{
    public class RootOrderResponse
    {
        public List<OrderModel> Data { get; set; }
        public string Exception { get; set; }
        public string SuccessMessage { get; set; }
        public int ReturnID { get; set; }
        public int Action { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class OrderParentSharing
    {
        public string DealerId { get; set; }
        public decimal Sharing { get; set; }
    }

    public class OrderModel
    {
        public string OrderId { get; set; }
        public string Device { get; set; }
        public int SymbolId { get; set; }
        public string SymbolName { get; set; }
        public int SecurityId { get; set; }
        public int SymbolDigit { get; set; }
        public string Side { get; set; }

        public DateTime? SymbolExpiry { get; set; }
        public DateTime? SymbolExpiryClose { get; set; }

        public double SymbolContractSize { get; set; }
        public double CurrentPrice { get; set; }
        public string Reason { get; set; }
        public string ClientIp { get; set; }

        public decimal Margin { get; set; }
        public double Price { get; set; }
        public double Volume { get; set; }

        public List<OrderParentSharing> ParentSharing { get; set; }

        public DateTime CreatedAt { get; set; }

        public string MasterSymbolName { get; set; }
        public string OrderType { get; set; }
        public string MarginType { get; set; }
        public string OrderFulfillment { get; set; }
        public string Comment { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string SecurityName { get; set; }
        public string SymbolDetail { get; set; }

        public string SpreadType { get; set; }
        public double SpreadValue { get; set; }
        public double SpreadBalance { get; set; }

        public string OperatorId { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
    }
}
