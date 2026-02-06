namespace ClientDesktop.Core.Models
{
    public class RootOrderResponse
    {
        public List<OrderModel> data { get; set; }
        public string exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class OrderParentSharing
    {
        public string dealerId { get; set; }
        public decimal sharing { get; set; }
    }

    public class OrderModel
    {
        public string orderId { get; set; }
        public string device { get; set; }
        public int symbolId { get; set; }
        public string symbolName { get; set; }
        public int securityId { get; set; }
        public int symbolDigit { get; set; }
        public string side { get; set; }
        public DateTime? symbolExpiry { get; set; }
        public DateTime? symbolExpiryClose { get; set; }
        public double symbolContractSize { get; set; }
        public double currentPrice { get; set; }
        public string reason { get; set; }
        public string clientIp { get; set; }
        public decimal margin { get; set; }
        public double price { get; set; }
        public double volume { get; set; }
        public List<OrderParentSharing> parentSharing { get; set; }
        public DateTime createdAt { get; set; }
        public string masterSymbolName { get; set; }
        public string orderType { get; set; }
        public string marginType { get; set; }
        public string orderFulfillment { get; set; }
        public string comment { get; set; }
        public DateTime updatedAt { get; set; }
        public string securityName { get; set; }
        public string symbolDetail { get; set; }
        public string spreadType { get; set; }
        public double spreadValue { get; set; }
        public double spreadBalance { get; set; }
        public string operatorId { get; set; }
        public string username { get; set; }
        public string userId { get; set; }
    }
}
