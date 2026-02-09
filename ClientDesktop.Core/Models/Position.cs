namespace ClientDesktop.Core.Models
{
    public class RootPositionResponse
    {
        public List<Position> Data { get; set; }
        public string Exception { get; set; }
        public string SuccessMessage { get; set; }
        public int ReturnID { get; set; }
        public int Action { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class Position
    {
        public string Id { get; set; }
        public int SymbolId { get; set; }
        public string SymbolName { get; set; }
        public string SymbolDetail { get; set; }
        public int SecurityId { get; set; }
        public int SymbolDigit { get; set; }
        public string Side { get; set; }

        public DateTime? SymbolExpiry { get; set; }
        public DateTime? SymbolExpiryClose { get; set; }

        public double SymbolContractSize { get; set; }
        public double AveragePrice { get; set; }
        public decimal? AverageOutPrice { get; set; }
        public double CurrentPrice { get; set; }
        public string Status { get; set; }
        public decimal? Margin { get; set; }
        public int? OrderCount { get; set; }
        public double InVolume { get; set; }
        public decimal OutVolume { get; set; }
        public double TotalVolume { get; set; }
        public string Reason { get; set; }
        public string ClientIp { get; set; }
        public string Device { get; set; }

        public DateTime? LastInAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastOutAt { get; set; }

        public string RefId { get; set; }
        public string MasterSymbolName { get; set; }
        public decimal? Pnl { get; set; }
        public string Comment { get; set; }
        public string MarginType { get; set; }
        public decimal? MarginRate { get; set; }
        public bool WeeklyRollOver { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? FirstPositionCreatedDate { get; set; }
        public string SpreadType { get; set; }
        public double? SpreadValue { get; set; }
        public double? SpreadBalance { get; set; }

        public List<ParentSharing> ParentSharing { get; set; }
        public List<string> Parents { get; set; }

        public string OperatorId { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
    }

}