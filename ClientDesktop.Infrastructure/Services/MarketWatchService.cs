using ClientDesktop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraderApp.Interfaces;
using TraderApp.Utils.Network;
using TraderApps.Config;
using TraderApps.Helpers;
using TraderApps.Utils.Storage;

namespace TraderApp.Services
{
    public class MarketWatchService
    {
        private readonly IApiService _apiService;
        private readonly IRepository<MarketWatchData> _repo;

        public MarketWatchService()
        {
            _apiService = new ApiService();
            _repo = new FileRepository<MarketWatchData>();
        }

        public async Task<MarketWatchData> GetMarketWatchDataAsync(bool forceApiSync = false)
        {
            try
            {
                string folderName = AESHelper.ToBase64UrlSafe(SessionManager.LicenseId);
                string fileName = AESHelper.ToBase64UrlSafe(SessionManager.UserId);
                string relativePath = System.IO.Path.Combine(folderName, fileName);

                var cachedData = _repo.Load(relativePath, "symbol");

                if (!forceApiSync && cachedData != null)
                {
                    return cachedData;
                }

                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchInitDataUrl);
                    var apiResponse = await _apiService.GetAsync<MarketWatchApiResponse>(url);

                    if (apiResponse != null && apiResponse.isSuccess && apiResponse.data != null)
                    {
                        _repo.Save(relativePath, apiResponse.data, "symbol");
                        return apiResponse.data;
                    }
                    else
                    {
                        FileLogger.Log("MarketWatchService", "API call failed or returned empty. Using Cache.");
                    }
                }
                else
                {
                    FileLogger.Log("Network", "No Internet Connection. Loading Local Data.");
                }

                return cachedData;
            }
            catch (Exception ex)
            {
                FileLogger.Log("MarketWatchService", $"Error: {ex.Message}");
                return null;
            }
        }

        public async Task<HideSymbolResponse> SaveProfileAsync(object payload)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchSaveClientProfileUrl);
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
                string url = CommonHelper.ToReplaceUrl(AppConfig.MarketWatchHideApiUrl);
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
