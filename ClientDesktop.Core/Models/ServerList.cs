namespace ClientDesktop.Core.Models
{
    public class ServerListResponse
    {
        public ServerListData data { get; set; }
        public object exception { get; set; }
        public string successMessage { get; set; }
        public int returnID { get; set; }
        public int action { get; set; }
        public bool isSuccess { get; set; }
    }

    public class ServerListData
    {
        public int licenseVesionID { get; set; }
        public string operatorID { get; set; }
        public object licenseDetails { get; set; }
        public int? licenseID { get; set; }

        public List<ServerList> licenseDetail { get; set; }
    }

    public class ServerList
    {
        public int licenseId { get; set; }
        public string serverDisplayName { get; set; }
        public string companyName { get; set; }
        public string primaryDomain { get; set; }
        public string secondaryDomain { get; set; }
        public string accessDomain { get; set; }
        public string serverIP { get; set; }
        public string licenseType { get; set; }
        public string licenseLogo { get; set; }
        public string parentsDomain { get; set; }
        public string operatorId { get; set; }
        public bool licenseStatus { get; set; }
    }
}
