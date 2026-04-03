using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using Newtonsoft.Json.Linq;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Handles all trade-related HTTP operations and local market-watch data retrieval.
    /// </summary>
    public class TradeService : ITradeService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<MarketWatchData> _marketWatchRepo;

        /// <param name="apiService">HTTP abstraction for API calls.</param>
        /// <param name="sessionService">Provides session context (userId, domain, licenseId).</param>
        public TradeService(IApiService apiService, SessionService sessionService)
        {
            try
            {
                _apiService = apiService;
                _sessionService = sessionService;
                _marketWatchRepo = new FileRepository<MarketWatchData>();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(TradeService), ex);
            }
        }

        /// <inheritdoc/>
        public async Task<MarketWatchData> GetMarketWatchDataAsync()
        {
            try
            {
                string folderName = AESHelper.ToBase64UrlSafe(_sessionService.LicenseId);
                string fileName = AESHelper.ToBase64UrlSafe(_sessionService.UserId);
                string relativePath = Path.Combine(folderName, fileName);

                var cachedData = _marketWatchRepo.Load(relativePath, "marketwatch");
                return cachedData ?? new MarketWatchData();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetMarketWatchDataAsync), ex);
                return new MarketWatchData();
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string ErrorMessage, SymbolData SymbolData)> GetSymbolDataAsync(
            string clientId, int symbolId)
        {
            try
            {
                var url = CommonHelper.ToReplaceUrl(
                    $"{AppConfig.GetSymbolDataForTrade}/{clientId}/{symbolId}",
                    _sessionService.PrimaryDomain);

                var responseData = await _apiService.GetAsync<SymbolDataResponse>(url);

                if (responseData?.Data == null)
                    return (false, "Failed to retrieve symbol details.", null); // ← was incorrectly (true, ...)

                return (true, null, responseData.Data.FirstOrDefault());
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetSymbolDataAsync), ex);
                return (false, ex.Message, null);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteOrderAsync(string orderId)
        {
            try
            {
                var payload = new
                {
                    orderIds = new[] { orderId },
                    deviceDetail = new
                    {
                        clientIP = CommonHelper.GetLocalIPAddress(),
                        device = "web",
                        reason = "Client"
                    }
                };

                var result = await _apiService.DeleteAsync<JObject>(
                    CommonHelper.ToReplaceUrl(AppConfig.DeleteOrderURL, _sessionService.PrimaryDomain),
                    payload);

                return result?["isSuccess"]?.Value<bool>() ?? false;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(DeleteOrderAsync), ex);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string ErrorMessage, string TradeMessage)> PlaceOrModifyOrderAsync(
            object payload, bool isModify = false)
        {
            try
            {
                var url = CommonHelper.ToReplaceUrl(AppConfig.TradeOrderURL, _sessionService.PrimaryDomain);

                // Removed dead StringContent — _apiService serialises payload internally
                TradeOrderResponse response = isModify
                    ? await _apiService.PutAsync<TradeOrderResponse>(url, payload)
                    : await _apiService.PostAsync<TradeOrderResponse>(url, payload);

                if (response.isSuccess)
                    return (true, null, isModify ? "Order Modified Successfully!" : "Order Placed Successfully!");

                return (false, response.exception.message, null);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PlaceOrModifyOrderAsync), ex);
                return (false, ex.Message, null);
            }
        }
    }
}