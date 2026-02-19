using ClientDesktop.Core.Models;

namespace ClientDesktop.Infrastructure.Services
{
    public class SessionService
    {
        #region Variables And Properties

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        public string Token { get; private set; }
        public string UserId { get; private set; }
        public string Username { get; private set; }
        public string LicenseId { get; private set; }
        public string PrimaryDomain { get; private set; }
        public DateTime? Expiration { get; private set; }
        public List<ServerList> ServerListData { get; private set; }
        public SocketLoginInfo socketLoginInfos { get; set; }
        public bool IsClientDataLoaded { get; set; } = false;
        public bool IsPasswordReadOnly { get; set; } = false;
        public List<MarketWatchApiSymbol> SymbolNameList { get; set; }
        public List<ClientDetails> ClientListData { get; private set; }
        public string Password { get; private set; }
        public double LastSelectedQty { get; set; }
        public (string UserId, string password, string LicenseId) LastSelectedLogin { get; set; }

        #endregion

        #region Session Management
        public void SetSession(string token, string userId, string username, string licenseId, DateTime? expiration, string password)
        {
            Token = token;
            UserId = userId;
            Username = username;
            LicenseId = licenseId;
            Expiration = expiration;
            Password = password;
            PrimaryDomain = ServerListData?.FirstOrDefault(w => w.licenseId.ToString() == licenseId)?.primaryDomain ?? string.Empty;
        }

        public void SetServerList(List<ServerList> list)
        {
            ServerListData = list;
        }

        public void SetClientList(List<ClientDetails> clients)
        {
            ClientListData = clients;
            TriggerLogin();
        }

        public void ClearSession()
        {
            Token = null;
            Username = null;
            Expiration = null;
            PrimaryDomain = null;
            TriggerLogout();
        }
        #endregion

        #region Events
        public event Action OnLoginSuccess;
        public event Action OnLogout;

        private void TriggerLogin() => OnLoginSuccess?.Invoke();
        private void TriggerLogout() => OnLogout?.Invoke();
        #endregion
    }
}
