using Newtonsoft.Json;

namespace ClientDesktop.Core.Models
{
    public class Ledgermodel
    {
        [JsonProperty("ledgerTransactionId")]
        public long LedgerTransactionId { get; set; }

        [JsonProperty("operatorId")]
        public string OperatorId { get; set; }

        [JsonProperty("userRole")]
        public string UserRole { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("parentUserName")]
        public string ParentUserName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("transactionType")]
        public string TransactionType { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [JsonProperty("ledgerDate")]
        public DateTime LedgerDate { get; set; }
    }
    public class LedgerResponse
    {
        [JsonProperty("data")]
        public LedgerData data { get; set; }
        
        [JsonProperty("exception")]
        public string exception { get; set; }

        [JsonProperty("successMessage")]
        public string successMessage { get; set; }

        [JsonProperty("returnID")]
        public int returnID { get; set; }

        [JsonProperty("action")]
        public int action { get; set; }

        [JsonProperty("isSuccess")]
        public bool isSuccess { get; set; }
    }

    public class LedgerData
    {
        [JsonProperty("openingAmount")]
        public decimal OpeningAmount { get; set; }

        [JsonProperty("closingAmount")]
        public decimal ClosingAmount { get; set; }

        [JsonProperty("transactions")]
        public List<Ledgermodel> Transactions { get; set; }
    }
    public class LedgerAuthResponse
    {
        [JsonProperty("data")]
        public LedgerAuthData data { get; set; }

        [JsonProperty("exception")]
        public string exception { get; set; }

        [JsonProperty("successMessage")]
        public string successMessage { get; set; }

        [JsonProperty("returnID")]
        public int returnID { get; set; }

        [JsonProperty("action")]
        public int action { get; set; }

        [JsonProperty("isSuccess")]
        public bool isSuccess { get; set; }
    }

    public class LedgerAuthData
    {
        [JsonProperty("status")]
        public bool status { get; set; }

        [JsonProperty("isSuccess")]
        public int isSuccess { get; set; }

        [JsonProperty("msg")]
        public List<string> msg { get; set; }
    }

    public class LedgerUserResponse
    {
        [JsonProperty("data")]
        public LedgerUserDetail data { get; set; }

        [JsonProperty("exception")]
        public string exception { get; set; }

        [JsonProperty("successMessage")]
        public string successMessage { get; set; }

        [JsonProperty("returnID")]
        public int returnID { get; set; }

        [JsonProperty("action")]
        public int action { get; set; }

        [JsonProperty("isSuccess")]
        public bool isSuccess { get; set; }
    }

    public class LedgerUserDetail
    {
        [JsonProperty("userRole")]
        public string UserRole { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("remarks")]
        public string Remarks { get; set; } // Can be null

        [JsonProperty("clientDeleted")]
        public bool ClientDeleted { get; set; }

        [JsonProperty("dealerDeleted")]
        public bool DealerDeleted { get; set; }
    }
    public class LedgerRowModel
    {
        public string Sr { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;  
        public bool IsSummaryRow { get; set; } = false;
    }
}
