namespace ClientDesktop.Core.Models
{
    public class HistoryModel
    {
        public int clientDealId { get; set; }
        public string operatorId { get; set; }
        public string clientId { get; set; }
        public string clientName { get; set; }
        public string dealerId { get; set; }
        public string orderId { get; set; }
        public string symbolName { get; set; }
        public string orderType { get; set; }
        public string dealType { get; set; }
        public string side { get; set; }
        public decimal price { get; set; }
        public decimal volume { get; set; }
        public decimal currentPrice { get; set; }
        public string dealStatus { get; set; }
        public decimal inVolume { get; set; }
        public decimal outVolume { get; set; }
        public decimal pnl { get; set; }
        public decimal uplineCommission { get; set; }
        public string clientIp { get; set; }
        public string device { get; set; }
        public string reason { get; set; }
        public string comment { get; set; }
        public DateTime createdOn { get; set; }
        public string symbolDetail { get; set; }
        public int symbolDigits { get; set; }
        public string refId { get; set; }
        public string positionId { get; set; }
        public string currency { get; set; }
        public decimal fee { get; set; }
        public decimal swap { get; set; }
        public decimal sl { get; set; }
        public decimal tp { get; set; }
        public int refIDAuto { get; set; }
        public int orderIDAuto { get; set; }
        public int positionIDAuto { get; set; }
    }

    public class HistoryResponse
    {
        public List<HistoryModel> data { get; set; }
        public bool isSuccess { get; set; }
        public string exception { get; set; }
        public string successMessage { get; set; }
    }
}
