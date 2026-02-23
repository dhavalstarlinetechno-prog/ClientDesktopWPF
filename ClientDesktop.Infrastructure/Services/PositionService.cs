using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Services
{
    public class PositionService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<Position>> _positionRepo;
        private readonly IRepository<List<OrderModel>> _orderRepo;

        public PositionService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
            _positionRepo = new FileRepository<List<Position>>();
            _orderRepo = new FileRepository<List<OrderModel>>();
        }

        #region Public Methods (Main Logic)

        public async Task<(bool Success, string ErrorMessage, List<Position> Positions)> GetPositionsAsync()
        {
            // 1. Load Local
            var cachedPositions = await Task.Run(() => LoadStoredPositions());

            try
            {
                // 2. Call API
                string url = CommonHelper.ToReplaceUrl(AppConfig.GetPositionsForClient, _sessionService.PrimaryDomain);
                var apiResponse = await _apiService.GetAsync<RootPositionResponse>(url);

                // 3. Check API Failure
                if (apiResponse == null || !apiResponse.IsSuccess)
                {
                    if (cachedPositions != null && cachedPositions.Count > 0)
                        return (true, "API Failed, Loaded from Cache", cachedPositions);

                    return (false, "Failed to load positions", null);
                }

                // 4. API Success -> Save Local -> Return New Data
                if (apiResponse.Data != null)
                {
                    SaveStoredPositions(apiResponse.Data);
                    return (true, null, apiResponse.Data);
                }

                return (true, null, new List<Position>());
            }
            catch (Exception ex)
            {
                FileLogger.Log("PositionService", $"GetPositions Error: {ex.Message}");

                // Fallback to Cache on Exception
                if (cachedPositions != null && cachedPositions.Count > 0)
                    return (true, null, cachedPositions);

                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string ErrorMessage, List<OrderModel> Orders)> GetOrdersAsync()
        {
            // 1. Load Local
            var cachedOrders = await Task.Run(() => LoadStoredOrders());

            try
            {
                // 2. Call API
                string url = CommonHelper.ToReplaceUrl(AppConfig.PositionOrderApiUrl, _sessionService.PrimaryDomain);
                var apiResponse = await _apiService.GetAsync<RootOrderResponse>(url);

                // 3. Check API Failure
                if (apiResponse == null || !apiResponse.IsSuccess)
                {
                    if (cachedOrders != null && cachedOrders.Count > 0)
                        return (true, "API Failed, Loaded from Cache", cachedOrders);

                    return (false, "Failed to load orders", null);
                }

                // 4. API Success -> Save Local -> Return New Data
                if (apiResponse.Data != null)
                {
                    SaveStoredOrders(apiResponse.Data);
                    return (true, null, apiResponse.Data);
                }

                return (true, null, new List<OrderModel>());
            }
            catch (Exception ex)
            {
                FileLogger.Log("PositionService", $"GetOrders Error: {ex.Message}");

                // Fallback to Cache on Exception
                if (cachedOrders != null && cachedOrders.Count > 0)
                    return (true, null, cachedOrders);

                return (false, ex.Message, null);
            }
        }

        #endregion

        #region Private Helpers (Separated Logic)

        // Common Path Generator (Logic ek jagah)
        private string GetStoragePath()
        {
            string domain = _sessionService.ServerListData
                .FirstOrDefault(w => w.licenseId.ToString() == _sessionService.LicenseId)?
                .serverDisplayName;

            return Path.Combine(
                AESHelper.ToBase64UrlSafe(domain),
                AESHelper.ToBase64UrlSafe(_sessionService.UserId)
            );
        }

        // --- POSITION METHODS ---

        private List<Position> LoadStoredPositions()
        {
            return _positionRepo.Load(GetStoragePath(), "position");
        }

        private void SaveStoredPositions(List<Position> data)
        {
            _positionRepo.Save(GetStoragePath(), data, "position");
        }

        // --- ORDER METHODS ---

        private List<OrderModel> LoadStoredOrders()
        {
            return _orderRepo.Load(GetStoragePath(), "order");
        }

        private void SaveStoredOrders(List<OrderModel> data)
        {
            _orderRepo.Save(GetStoragePath(), data, "order");
        }

        #endregion
    }
}