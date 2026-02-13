namespace ClientDesktop.Core.Models
{
    public class PositionHistoryModel
    {
        public string Id { get; set; }
        public int SymbolId { get; set; }
        public string SymbolName { get; set; }
        public string SymbolDetail { get; set; }
        public int SecurityId { get; set; }
        public int SymbolDigit { get; set; }
        public string Side { get; set; }
        public DateTime SymbolExpiry { get; set; }
        public DateTime SymbolExpiryClose { get; set; }
        public int SymbolContractSize { get; set; }
        public double AveragePrice { get; set; }
        public double AverageOutPrice { get; set; }
        public double CurrentPrice { get; set; }
        public string Status { get; set; }
        public double Margin { get; set; }
        public int OrderCount { get; set; }
        public double InVolume { get; set; }
        public double OutVolume { get; set; }
        public double TotalVolume { get; set; }
        public string VolumeDisplay => $"{OutVolume}/{InVolume}";
        public string Reason { get; set; }
        public string ClientIp { get; set; }
        public string Device { get; set; }
        public DateTime LastInAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastOutAt { get; set; }
        public string RefId { get; set; }
        public string MasterSymbolName { get; set; }
        public double Pnl { get; set; }
        public string Comment { get; set; }
        public string MarginType { get; set; }
        public double MarginRate { get; set; }
        public bool WeeklyRollOver { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FirstPositionCreatedDate { get; set; }
        public string SpreadType { get; set; }
        public double SpreadValue { get; set; }
        public double SpreadBalance { get; set; }
        public string ParentSharing { get; set; }
        public object Parents { get; set; }
        public string OperatorId { get; set; }
        public string Username { get; set; }
        public string UserId { get; set; }
    }

    public class PositionHistoryResponse
    {
        public List<PositionHistoryModel> Data { get; set; }
        public object Exception { get; set; }
        public string SuccessMessage { get; set; }
        public int ReturnId { get; set; }
        public int Action { get; set; }
        public bool IsSuccess { get; set; }
    }
}