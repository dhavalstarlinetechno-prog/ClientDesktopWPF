namespace ClientDesktop.Core.Models
{
    public class Invoicemodel
    {
        public string SymbolName { get; set; }
        public string SecurityName { get; set; }
        public string OrderType { get; set; }
        public string DealType { get; set; }
        public string Side { get; set; }
        public string Reason { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public double UplineCommission { get; set; }
        public double Pnl { get; set; }
        public DateTime DealCreatedOn { get; set; }
    }
}
