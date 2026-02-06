using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClientDesktop.Core.Models
{
    public class ClientDetails
    {
        public int ClientMasterId { get; set; }
        public string OperatorId { get; set; }
        public string ClientId { get; set; }
        public string DealerId { get; set; }
        public string ClientName { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string Currency { get; set; }
        public bool EnablePasswordChange { get; set; }
        public bool EnableTrading { get; set; }
        public bool CloseOnlyTradeLock { get; set; }
        public bool RealtimeCommission { get; set; }
        public string FreeMargin { get; set; }
        public bool AllowedOnlinePayment { get; set; }
        public bool ClientStatus { get; set; }
        public double UplineAmount { get; set; }
        public double CreditAmount { get; set; }
        public double Pnl { get; set; }
        public double FreeMarginAmount { get; set; }
        public double OccupiedMarginAmount { get; set; }
        public double FloatingPLAmount { get; set; }
        public double UplineCommission { get; set; }
        public List<int> ClientGroups { get; set; }
        public List<int> SymbolRoutes { get; set; }
        public double Balance { get; set; }
        public bool IsViewLocked { get; set; }
        public bool IsViewLockedInvester { get; set; }
    }

    public class TimeZoneDetails
    {
        public string Value { get; set; }
        public string Label { get; set; }
        public double Offset { get; set; }
        public string Abbrev { get; set; }
        public string AltName { get; set; }
    }

    public class ClientDetailsRootModel
    {
        [JsonConverter(typeof(ClientDetailsFlexibleConverter))]
        public ClientDetails data { get; set; }

        public object exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class ClientDataWrapper
    {
        public ClientDetails clientDetails { get; set; }
        public TimeZoneDetails timeZoneDetails { get; set; }
    }

    public class ClientDetailsFlexibleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(ClientDetails);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);

            // CASE 1: data → clientDetails
            if (json["clientDetails"] != null)
                return json["clientDetails"].ToObject<ClientDetails>(serializer);

            // CASE 2: data itself is ClientDetails
            return json.ToObject<ClientDetails>(serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

}
