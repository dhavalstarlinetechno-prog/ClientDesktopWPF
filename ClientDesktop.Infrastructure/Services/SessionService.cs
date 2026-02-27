using ClientDesktop.Core.Events;
using ClientDesktop.Core.Models;
using CommunityToolkit.Mvvm.Messaging;
using System.Net.NetworkInformation;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for holding and managing global session state for the authenticated user.
    /// </summary>
    public class SessionService
    {
        #region Variables And Properties

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        public bool IsInternetAvailable { get; set; }
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

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SessionService class and sets the initial network availability state.
        /// </summary>
        public SessionService()
        {
            IsInternetAvailable = NetworkInterface.GetIsNetworkAvailable();

            WeakReferenceMessenger.Default.Register<NetworkStateEvent>(this, (recipient, message) =>
            {
                IsInternetAvailable = message.IsConnected;
            });
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Sets the active session data for the logged-in user.
        /// </summary>
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

        /// <summary>
        /// Sets the available server list data.
        /// </summary>
        public void SetServerList(List<ServerList> list)
        {
            ServerListData = list;
        }

        /// <summary>
        /// Sets the client details list for the active session.
        /// </summary>
        public void SetClientList(List<ClientDetails> clients)
        {
            ClientListData = clients;
        }

        /// <summary>
        /// Clears the active session data effectively logging out the user.
        /// </summary>
        public void ClearSession()
        {
            Token = null;
            Username = null;
            Expiration = null;
            PrimaryDomain = null;
        }

        #endregion
    }
}