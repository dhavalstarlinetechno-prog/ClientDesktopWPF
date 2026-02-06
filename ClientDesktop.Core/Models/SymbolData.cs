namespace ClientDesktop.Core.Models
{
    public class SymbolData
    {
        public int ClientSymbolId { get; set; }
        public int GroupId { get; set; }
        public string ClientId { get; set; }
        public string DealerId { get; set; }
        public int RouteId { get; set; }
        public string RoutePath { get; set; }
        public int SymbolId { get; set; }
        public string SymbolName { get; set; }
        public string MasterSymbolName { get; set; }
        public int SecurityId { get; set; }
        public bool GroupTradeDefaultSet { get; set; }
        public string SymbolTrade { get; set; }
        public bool GroupOrderDefaultSet { get; set; }
        public string SymbolOrder { get; set; }
        public bool GroupMinMaxDefaultSet { get; set; }
        public double SymbolMinimumValue { get; set; }
        public double SymbolStepValue { get; set; }
        public double SymbolOneClickValue { get; set; }
        public double SymbolTotalValue { get; set; }
        public bool GroupMarginDefaultSet { get; set; }
        public string SymbolMarginType { get; set; }
        public double SymbolTradeMargin { get; set; }
        public bool MarginOnLimit { get; set; }
        public string SpreadType { get; set; }
        public double SpreadValue { get; set; }
        public double SpreadBalance { get; set; }
        public bool SymbolConfigStatus { get; set; }
        public string SecurityGTC { get; set; }
        public int SymbolDigits { get; set; }
        public bool IntraDay { get; set; }
        public int SymbolLimitstoplevel { get; set; }
        public int SymbolOffOdds { get; set; }
        public bool CloseOnlyTradeLock { get; set; }
        public bool SymbolAdvanceLimit { get; set; }
        public bool SymbolStatus { get; set; }
        public object Symbols { get; set; }
    }

    public class SymbolDataResponse
    {
        public List<SymbolData> Data { get; set; }
        public object Exception { get; set; }
        public string SuccessMessage { get; set; }
        public int ReturnID { get; set; }
        public int Action { get; set; }
        public bool IsSuccess { get; set; }
    }
}
