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
                FileLogger.ApplicationLog(nameof(GetPositionsAsync), $"GetPositions Error: {ex.Message}");

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
                FileLogger.ApplicationLog(nameof(GetOrdersAsync), $"GetOrders Error: {ex.Message}");

                // Fallback to Cache on Exception
                if (cachedOrders != null && cachedOrders.Count > 0)
                    return (true, null, cachedOrders);

                return (false, ex.Message, null);
            }
        }

        #endregion

        #region Local Cache Specific Methods

        public List<Position> GetCachedPositions()
        {
            return LoadStoredPositions() ?? new List<Position>();
        }

        public List<OrderModel> GetCachedOrders()
        {
            return LoadStoredOrders() ?? new List<OrderModel>();
        }

        public void UpdateLocalPosition(Position position, bool isDeleted)
        {
            try
            {
                var list = GetCachedPositions();
                if (isDeleted)
                {
                    list.RemoveAll(p => p.Id == position.Id);
                }
                else
                {
                    var existingIndex = list.FindIndex(p => p.Id == position.Id);
                    if (existingIndex >= 0)
                        list[existingIndex] = position; // Update
                    else
                        list.Insert(0, position);       // Add
                }
                SaveStoredPositions(list);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateLocalPosition), $"UpdateLocalPosition Error: {ex.Message}");
            }
        }

        public void UpdateLocalOrder(OrderModel order, bool isDeleted)
        {
            try
            {
                var list = GetCachedOrders();
                if (isDeleted)
                {
                    list.RemoveAll(o => o.OrderId == order.OrderId);
                }
                else
                {
                    var existingIndex = list.FindIndex(o => o.OrderId == order.OrderId);
                    if (existingIndex >= 0)
                        list[existingIndex] = order;    // Update
                    else
                        list.Insert(0, order);          // Add
                }
                SaveStoredOrders(list);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateLocalOrder), $"UpdateLocalOrder Error: {ex.Message}");
            }
        }

        #endregion

        #region Private Helpers (Separated Logic)

        // Common Path Generator
        private string GetStoragePath()
        {
            string folderName = AESHelper.ToBase64UrlSafe(_sessionService.LicenseId);
            string fileName = AESHelper.ToBase64UrlSafe(_sessionService.UserId);
            string relativePath = Path.Combine(folderName, fileName);

            return relativePath;
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