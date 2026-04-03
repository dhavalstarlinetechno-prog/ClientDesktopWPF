using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for managing market watch data, user profiles, and symbol visibility.
    /// </summary>
    public class MarketWatchService
    {
        #region Fields

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<MarketWatchData> _repo;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the market watch data is successfully updated from the API.
        /// </summary>
        public event Action<MarketWatchData> OnDataUpdated;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MarketWatchService class.
        /// </summary>
        public MarketWatchService(IApiService apiService, SessionService sessionService)
        {
            try
            {
                _apiService = apiService;
                _sessionService = sessionService;
                _repo = new FileRepository<MarketWatchData>();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MarketWatchService), ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves market watch data from the local repository or API based on the sync flag.
        /// </summary>
        public async Task<MarketWatchData> GetMarketWatchDataAsync(bool forceApiSync = false)
        {
            try
            {
                string folderName = AESHelper.ToBase64UrlSafe(_sessionService.LicenseId);
                string fileName = AESHelper.ToBase64UrlSafe(_sessionService.UserId);
                string relativePath = Path.Combine(folderName, fileName);

                var cachedData = _repo.Load(relativePath, "marketwatch");

                if (!forceApiSync && cachedData != null)
                {
                    return cachedData;
                }

                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchInitDataUrl, _sessionService.PrimaryDomain);
                    var apiData = await _apiService.GetAsync<MarketWatchApiResponse>(url);

                    if (apiData != null && apiData.data != null)
                    {
                        Task.Run(() => _repo.Save(relativePath, apiData.data, "marketwatch"));

                        if (forceApiSync)
                        {
                            OnDataUpdated?.Invoke(apiData.data);
                        }

                        return apiData.data;
                    }
                }
                else
                {
                    FileLogger.ApplicationLog(nameof(GetMarketWatchDataAsync), "No Internet Connection. Loading Local Data.");
                }

                return cachedData ?? new MarketWatchData();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetMarketWatchDataAsync), ex);
                return new MarketWatchData();
            }
        }

        /// <summary>
        /// Saves the client's market watch profile configuration to the server.
        /// </summary>
        public async Task<HideSymbolResponse> SaveProfileAsync(object payload)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchSaveClientProfileUrl, _sessionService.PrimaryDomain);
                return await _apiService.PostAsync<HideSymbolResponse>(url, payload);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SaveProfileAsync), ex);
                return null;
            }
        }

        /// <summary>
        /// Sends a request to the server to hide specific symbols for the user.
        /// </summary>
        public async Task<HideSymbolResponse> HideSymbolsAsync(List<int> symbolIds)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchHideApiUrl, _sessionService.PrimaryDomain);
                var payload = new { symbolId = symbolIds };
                return await _apiService.PutAsync<HideSymbolResponse>(url, payload);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(HideSymbolsAsync), ex);
                return null;
            }
        }

        #endregion
    }
}