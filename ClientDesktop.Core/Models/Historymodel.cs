using System;
using System.Collections.Generic;

namespace ClientDesktop.Models
{
    public class HistoryModel
    {
        public int ClientDealId { get; set; }
        public string OperatorId { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string DealerId { get; set; }
        public string OrderId { get; set; }
        public string SymbolName { get; set; }
        public string OrderType { get; set; }
        public string DealType { get; set; }
        public string Side { get; set; }

        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal CurrentPrice { get; set; }

        public string DealStatus { get; set; }

        public decimal InVolume { get; set; }
        public decimal OutVolume { get; set; }
        public decimal Pnl { get; set; }
        public decimal UplineCommission { get; set; }

        public string ClientIp { get; set; }
        public string Device { get; set; }
        public string Reason { get; set; }
        public string Comment { get; set; }

        public DateTime CreatedOn { get; set; }

        public string SymbolDetail { get; set; }
        public int SymbolDigits { get; set; }

        public string RefId { get; set; }
        public string PositionId { get; set; }
        public string Currency { get; set; }

        public decimal Fee { get; set; }
        public decimal Swap { get; set; }
        public decimal Sl { get; set; }
        public decimal Tp { get; set; }

        public int RefIDAuto { get; set; }
        public int OrderIDAuto { get; set; }
        public int PositionIDAuto { get; set; }
    }

    public class HistoryResponse
    {
        public List<HistoryModel> Data { get; set; }
        public bool IsSuccess { get; set; }
        public string Exception { get; set; }
        public string SuccessMessage { get; set; }
    }
}
