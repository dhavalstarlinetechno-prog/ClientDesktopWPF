namespace ClientDesktop.Core.Models
{
    public class SessionModel
    {
        public string SessionDay { get; set; }
        public string Quotetime { get; set; }
        public string Tradetime { get; set; }
    }
    public class SymbolModel
    {
        public int SymbolId { get; set; }
        public string OperatorId { get; set; }
        public int SymbolRouteId { get; set; }
        public string SymbolRoutePath { get; set; }
        public string SymbolCode { get; set; }
        public string SymbolName { get; set; }
        public int SecurityId { get; set; }
        public string SecurityName { get; set; }
        public string MasterSymbolName { get; set; }
        public DateTime? SymbolExpiry { get; set; }
        public DateTime? SymbolExpiryclose { get; set; }
        public int SymbolDigits { get; set; }
        public string SecurityLimitpassby { get; set; }
        public bool SecurityPendingLimitbetweenhighlow { get; set; }
        public bool SecurityWeeklyrollover { get; set; }
        public string SecurityGtc { get; set; }
        public string SymbolChartmode { get; set; }
        public decimal SymbolContractsize { get; set; }
        public decimal SymbolTicksize { get; set; }
        public string SymbolTrade { get; set; }
        public string SymbolOrder { get; set; }
        public int SymbolLimitstoplevel { get; set; }
        public int SymbolOffodds { get; set; }
        public bool SymbolAdvancelimit { get; set; }
        public decimal SymbolMinimumvalue { get; set; }
        public decimal SymbolStepvalue { get; set; }
        public decimal SymbolOneclickvalue { get; set; }
        public decimal SymbolTotalvalue { get; set; }
        public string SymbolMargintype { get; set; }
        public decimal SymbolTrademargin { get; set; }
        public string SymbolDescription { get; set; }
        public bool SymbolStatus { get; set; }
        public bool MarginOnlimit { get; set; }
        public string RouteType { get; set; }
        public List<SessionModel> Sessions { get; set; }
    }
}
