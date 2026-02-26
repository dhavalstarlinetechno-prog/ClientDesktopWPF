using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.Infrastructure.Services
{
    public class TradeService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<MarketWatchData> _repo;

        public TradeService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
            _repo = new FileRepository<MarketWatchData>();
        }

        public async Task<MarketWatchData> GetMarketWatchDataAsync()
        {
            try
            {
                string folderName = AESHelper.ToBase64UrlSafe(_sessionService.LicenseId);
                string fileName = AESHelper.ToBase64UrlSafe(_sessionService.UserId);
                string relativePath = Path.Combine(folderName, fileName);

                var cachedData = _repo.Load(relativePath, "marketwatch");

                return cachedData ?? new MarketWatchData();
            }
            catch (Exception ex)
            {
                FileLogger.Log("MarketWatchService", $"Error: {ex.Message}");
                return new MarketWatchData();
            }
        }

        public async Task<(bool Success, string ErrorMessage, SymbolData SymbolData)> GetSymbolDataAsync(string clientId, int symbolId)
        {
            try
            {
                var url = CommonHelper.ToReplaceUrl($"{AppConfig.GetSymbolDataForTrade}/{clientId}/{symbolId}", _sessionService.PrimaryDomain);
                var responseData = await _apiService.GetAsync<SymbolDataResponse>(url);

                if (responseData == null || responseData.Data == null)
                {
                    return (true, "Failed to get Symbol details", null);
                }

                return (true, null, responseData.Data.FirstOrDefault());
            }
            catch (Exception ex)
            {
                return (true, ex.Message, null);
            }
        }
    }
}
