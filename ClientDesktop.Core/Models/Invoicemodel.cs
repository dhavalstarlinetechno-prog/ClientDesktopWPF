using Newtonsoft.Json;

namespace ClientDesktop.Core.Models
{

    public class Invoicemodel
    {
        [JsonProperty("symbolName")]
        public string SymbolName { get; set; }

        [JsonProperty("securityName")]
        public string SecurityName { get; set; }

        [JsonProperty("orderType")]
        public string OrderType { get; set; }

        [JsonProperty("dealType")]
        public string DealType { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("volume")]
        public double Volume { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("uplineCommission")]
        public double UplineCommission { get; set; }

        [JsonProperty("pnl")]
        public double Pnl { get; set; }

        [JsonProperty("dealCreatedOn")]
        public DateTime DealCreatedOn { get; set; }
    }

    public class SecurityRow
    {
        public string Date { get; set; }
        public string Type { get; set; }
        public string BVol { get; set; }
        public string SVol { get; set; }
        public string Rate { get; set; }
        public string Comm { get; set; }
        public string Net { get; set; }
        public bool IsHeader { get; set; }
        public bool IsTotal { get; set; }
    }
}
