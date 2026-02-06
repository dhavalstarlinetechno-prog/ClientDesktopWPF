namespace ClientDesktop.Core.Models
{
    public class HideSymbolResponse
    {
        public HideSymbolResponseData data { get; set; }
        public string exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class HideSymbolResponseData
    {
        public List<int> symbolId { get; set; }
    }
}
