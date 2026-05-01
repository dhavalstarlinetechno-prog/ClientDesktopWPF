using Newtonsoft.Json;

namespace ClientDesktop.Core.Models
{
    public class BanScriptResponse
    {
        [JsonProperty("data")]
        public List<BanScripts> BanScripts { get; set; } = new List<BanScripts>();
    }
    public class BanScripts
    {
        public int BanScriptId { get; set; }
        public string OperatorId { get; set; } = string.Empty;
        public int SecurityId { get; set; }
        public int MasterSymbolId { get; set; }
        public string MasterSymbolName { get; set; } = string.Empty;
        public string SymbolDisplayName { get; set; } = string.Empty;
        public DateTime BanDate { get; set; }
    }
}
