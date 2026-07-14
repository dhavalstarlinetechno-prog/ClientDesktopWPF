namespace ClientDesktop.Core.Models
{
    public class Chartmodel
    {
        public long time { get; set; }
        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public double volume { get; set; }
    }
    public class HistoryApiResponse
    {
        public bool isSuccess { get; set; }
        public List<HistoryApiBar> data { get; set; }
    }
    public class HistoryApiBar
    {
        public long updateTime { get; set; }
        public double openLtp { get; set; }
        public double highLtp { get; set; }
        public double lowLtp { get; set; }
        public double closeLtp { get; set; }
        public double volume { get; set; }
    }
}
