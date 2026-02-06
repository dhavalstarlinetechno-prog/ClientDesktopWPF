namespace ClientDesktop.Core.Models
{
    public class SubSymbolModel
    {
        public int SymbolId { get; set; }
        public int SymbolRouteId { get; set; }
        public string SymbolRoutePath { get; set; }
        public string SymbolCode { get; set; }
        public string SymbolName { get; set; }
        public bool SymbolStatus { get; set; }
        public string RouteType { get; set; }
        public DateTime? SymbolExpiry { get; set; } 
        public DateTime? SymbolExpiryClose { get; set; }
        public string SymbolDescription { get; set; }
    }
    public class SubSymbolRoot
    {
        public List<SubSymbolModel> Data { get; set; }
    }
    public class Symbolmodel
    {
        public List<Folder> Data { get; set; }
    }
    public class Folder
    {
        public int RouteId { get; set; }
        public string OperatorId { get; set; }
        public int ParentId { get; set; }
        public int FolderPosition { get; set; }
        public string FolderName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int SymbolCount { get; set; }
        public int CurrentRouteId { get; set; }
    }
    public class SubSymbolDisplayModel
    {
        public string SymbolName { get; set; }
        public DateTime? SymbolExpiry { get; set; }
        public DateTime? SymbolExpiryClose { get; set; }
        public string SymbolId { get; set; }
        public string SymbolStatus { get; set; }
    }

}
