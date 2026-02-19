using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Services
{
    public class MarketWatchService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<MarketWatchData> _repo;

        public event Action<MarketWatchData> OnDataUpdated;

        public MarketWatchService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
            _repo = new FileRepository<MarketWatchData>();
        }

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
                    FileLogger.Log("Network", "No Internet Connection. Loading Local Data.");
                }

                return cachedData ?? new MarketWatchData();
            }
            catch (Exception ex)
            {
                FileLogger.Log("MarketWatchService", $"Error: {ex.Message}");
                return new MarketWatchData();
            }
        }

        public async Task<HideSymbolResponse> SaveProfileAsync(object payload)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchSaveClientProfileUrl, _sessionService.PrimaryDomain);
                return await _apiService.PostAsync<HideSymbolResponse>(url, payload);
            }
            catch (Exception ex)
            {
                FileLogger.Log("MarketWatchService", $"SaveProfile Error: {ex.Message}");
                return null;
            }
        }

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
                FileLogger.Log("MarketWatchService", $"HideSymbol Error: {ex.Message}");
                return null;
            }
        }
    }
}
