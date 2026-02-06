namespace ClientDesktop.Core.Models
{
    public class BanScripts
    {
        public int BanScriptId { get; set; }
        public string OperatorId { get; set; }
        public int SecurityId { get; set; }
        public int MasterSymbolId { get; set; }
        public string MasterSymbolName { get; set; }
        public string SymbolDisplayName { get; set; }
        public DateTime BanDate { get; set; }
    }
}
