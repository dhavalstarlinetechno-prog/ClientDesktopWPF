using System;

namespace ClientDesktop.Core.Models
{
    public enum RowType
    {
        Position,
        Footer,
        Order
    }

    public class PositionGridRow
    {
        public string Id { get; set; }
        public string SymbolName { get; set; }
        public DateTime? Time { get; set; }
        public string Side { get; set; }
        public string OrderType { get; set; }

        public double? Volume { get; set; }
        public double? AveragePrice { get; set; }
        public double? CurrentPrice { get; set; }

        public decimal? Pnl { get; set; }
        public string Comment { get; set; }

        public RowType Type { get; set; }

        public bool IsFooter => Type == RowType.Footer;
        public bool IsOrder => Type == RowType.Order;
        public bool IsPosition => Type == RowType.Position;
    }
}