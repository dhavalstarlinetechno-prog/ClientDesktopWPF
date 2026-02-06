using ClientDesktop.Core.Models;

namespace ClientDesktop.Infrastructure.Services
{
    public static class SessionManager
    {
        #region Variables And Properties
        // In-memory
        public static string Token { get; private set; }
        public static string UserId { get; private set; }
        public static string Username { get; private set; }
        public static string LicenseId { get; private set; }
        public static DateTime? Expiration { get; private set; }
        public static List<ServerList> ServerListData { get; private set; }
        ////public static List<ClientDetails> ClientListData { get; private set; }
        public static SocketLoginInfo socketLoginInfos { get; set; }
        public static bool IsClientDataLoaded { get; set; } = false;
        public static bool IsPasswordReadOnly { get; set; } = false;
        public static List<MarketWatchApiSymbol> SymbolNameList { get; internal set; }

        public static List<ClientDetails> ClientListData { get; private set; }
        public static string Password { get; private set; }
        public static double LastSelectedQty { get; set; }
        public static (string UserId, string password, string LicenseId) LastSelectedLogin { get; set; }

        // File storage (encrypted) for "Remember Me"
        private static readonly string TokenFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClientDesktop", "session.dat");

        #endregion

        #region Session Management
        public static void SetSession(string token, string userId, string username, string licenseId, DateTime? expiration, string password)
        {
            Token = token;
            UserId = userId;
            Username = username;
            LicenseId = licenseId;
            Expiration = expiration;
            Password = password;
        }

        public static void SetServerList(List<ServerList> list)
        {
            ServerListData = list;
        }

        ////public static void SetClientList(List<ClientDetails> clients)
        ////{
        ////    ClientListData = clients;
        ////    SessionManager.socketLoginInfos.OperatorId = clients.FirstOrDefault().OperatorId;
        ////}

        public static void SetClientList(List<ClientDetails> clients)
        {
            ClientListData = clients;
        }

        public static void ClearSession()
        {
            Token = null;
            Username = null;
            //LicenseId = null;
            Expiration = null;
            //ServerListData = null;
        }
        #endregion
    }

    #region LoginInfo Class
    public class LoginInfo
    {
        // In-memory
        public string UserId { get; set; }
        public string Username { get; set; }
        public string LicenseId { get; set; }
        public DateTime? Expiration { get; set; }
        public List<ServerList> ServerListData { get; set; }
        public string Password { get; set; }
        public bool LastLogin { get; set; }
    }

    public class SocketLoginInfo
    {
        public string UserSubId { get; set; }
        public string UserIss { get; set; }
        public string LicenseId { get; set; }
        public string OperatorId { get; set; }
        public string Intime { get; set; }
        public string Role { get; set; }
        public string IpAddress { get; set; }
        public string Device { get; set; }
    }
    #endregion
}
