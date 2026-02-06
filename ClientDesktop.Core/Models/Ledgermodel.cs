namespace ClientDesktop.Core.Models
{
    public class Ledgermodel
    {
        public long LedgerTransactionId { get; set; }
        public string OperatorId { get; set; }
        public string UserRole { get; set; }
        public string UserName { get; set; }
        public string ParentUserName { get; set; }
        public string Name { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string Remarks { get; set; }
        public DateTime LedgerDate { get; set; }
    }
    public class LedgerResponse
    {
        public LedgerData data { get; set; }
        public string exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class LedgerData
    {
        public decimal OpeningAmount { get; set; }
        public decimal ClosingAmount { get; set; }
        public List<Ledgermodel> Transactions { get; set; }
    }
    public class LedgerAuthResponse
    {
        public LedgerAuthData data { get; set; }
        public string exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class LedgerAuthData
    {
        public bool status { get; set; }
        public int isSuccess { get; set; }
        public List<string> msg { get; set; }
    }

    public class LedgerUserDetail
    {
        public string UserRole { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string Remarks { get; set; } // Can be null
        public bool ClientDeleted { get; set; }
        public bool DealerDeleted { get; set; }
    }
}
