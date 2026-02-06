namespace ClientDesktop.Core.Models
{
    public class RootPositionResponse
    {
        public List<Position> data { get; set; }
        public string exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class Position
    {
        public string id { get; set; }
        public int symbolId { get; set; }
        public string symbolName { get; set; }
        public string symbolDetail { get; set; }
        public int securityId { get; set; }
        public int symbolDigit { get; set; }
        public string side { get; set; }

        // 👇 Nullable DateTimes to avoid "Error converting value {null}" issues
        public DateTime? symbolExpiry { get; set; }
        public DateTime? symbolExpiryClose { get; set; }

        public double symbolContractSize { get; set; }
        public double averagePrice { get; set; }
        public decimal? averageOutPrice { get; set; }
        public double currentPrice { get; set; }
        public string status { get; set; }
        public decimal? margin { get; set; }
        public int? orderCount { get; set; }
        public double inVolume { get; set; }
        public decimal outVolume { get; set; }
        public double totalVolume { get; set; }
        public string reason { get; set; }
        public string clientIp { get; set; }
        public string device { get; set; }

        public DateTime? lastInAt { get; set; }
        public DateTime? updatedAt { get; set; }
        public DateTime? lastOutAt { get; set; }

        public string refId { get; set; }
        public string masterSymbolName { get; set; }
        public decimal? pnl { get; set; }
        public string comment { get; set; }
        public string marginType { get; set; }
        public decimal? marginRate { get; set; }
        public bool weeklyRollOver { get; set; }

        public DateTime? createdAt { get; set; }
        public DateTime? firstPositionCreatedDate { get; set; }
        public string spreadType { get; set; }
        public double? spreadValue { get; set; }
        public double? spreadBalance { get; set; }

        public List<ParentSharing> parentSharing { get; set; }
        public List<string> parents { get; set; }

        public string operatorId { get; set; }
        public string username { get; set; }
        public string userId { get; set; }
    }
}