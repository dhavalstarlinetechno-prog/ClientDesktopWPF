using ClientDesktop.Core.Events;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
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
        public ClientDetails CurrentClient { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SessionService class and sets the initial network availability state.
        /// </summary>
        public SessionService()
        {
            try
            {
                IsInternetAvailable = NetworkInterface.GetIsNetworkAvailable();

                WeakReferenceMessenger.Default.Register<NetworkStateEvent>(this, (recipient, message) =>
                {
                    try
                    {
                        IsInternetAvailable = message.IsConnected;
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("NetworkStateEvent_Callback", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SessionService), ex);
            }
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Sets the active session data for the logged-in user.
        /// </summary>
        public void SetSession(string token, string userId, string username, string licenseId, DateTime? expiration, string password)
        {
            try
            {
                Token = token;
                UserId = userId;
                Username = username;
                LicenseId = licenseId;
                Expiration = expiration;
                Password = password;
                PrimaryDomain = ServerListData?.FirstOrDefault(w => w.licenseId.ToString() == licenseId)?.primaryDomain ?? string.Empty;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SetSession), ex);
            }
        }

        /// <summary>
        /// Sets the available server list data.
        /// </summary>
        public void SetServerList(List<ServerList> list)
        {
            try
            {
                ServerListData = list;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SetServerList), ex);
            }
        }

        /// <summary>
        /// Sets the client details list for the active session.
        /// </summary>
        public void SetClientList(List<ClientDetails> clients)
        {
            try
            {
                ClientListData = clients;

                CurrentClient = ClientListData?.FirstOrDefault(x => x.ClientId == UserId);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SetClientList), ex);
            }
        }

        /// <summary>
        /// Clears the active session data effectively logging out the user.
        /// </summary>
        public void ClearSession()
        {
            try
            {
                Token = null;
                Username = null;
                Expiration = null;
                PrimaryDomain = null;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ClearSession), ex);
            }
        }

        #endregion
    }
}