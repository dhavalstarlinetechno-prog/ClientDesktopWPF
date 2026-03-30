using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Models;
using Newtonsoft.Json;
using System.Text;

namespace ClientDesktop.Infrastructure.Services
{
    public class HistoryService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<HistoryModel>> _historyRepo;
        private readonly IRepository<List<PositionHistoryModel>> _positionHistoryRepo;

        public HistoryService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
            _historyRepo = new FileRepository<List<HistoryModel>>();
            _positionHistoryRepo = new FileRepository<List<PositionHistoryModel>>();
        }

        #region Core Data Loading Logic (Cache + API with Fallback)

        public List<HistoryModel> GetStoredHistory()
        {
            return _historyRepo.Load(GetStoragePath(), "History");
        }

        public List<PositionHistoryModel> GetStoredPositionHistory()
        {
            return _positionHistoryRepo.Load(GetStoragePath(), "PositionHistory");
        }

        #endregion  

        #region Private Helpers (File & Path)

        private string GetStoragePath()
        {
            return Path.Combine(AESHelper.ToBase64UrlSafe(_sessionService.LicenseId), AESHelper.ToBase64UrlSafe(_sessionService.UserId));
        }

        private void SaveStoredHistory(List<HistoryModel> historyList)
        {
            _historyRepo.Save(GetStoragePath(), historyList, "History");
        }

        private void SaveStoredPositionHistory(List<PositionHistoryModel> positionHistoryList)
        {
            _positionHistoryRepo.Save(GetStoragePath(), positionHistoryList, "PositionHistory");
        }

        #endregion

        #region API Calls

        public async Task<HistoryFetchResult> FetchHistoryFromApiAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var dealer = _sessionService.CurrentClient;
                if (dealer == null)
                {
                    FileLogger.ApplicationLog(nameof(FetchHistoryFromApiAsync), "DealerId not found.");
                    return TryLoadFromCache("DealerId missing");
                }

                var payload = new
                {
                    clientID = _sessionService.UserId,
                    dealerID = dealer.DealerId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService
                    .PostRawAsync(AppConfig.GetHistoryForClient.ToReplaceUrl(_sessionService.PrimaryDomain), content))
                {
                    if (response == null || response.Content == null)
                    {
                        FileLogger.ApplicationLog(nameof(FetchHistoryFromApiAsync), "Null response from API.");
                        return TryLoadFromCache("Null response from API");
                    }

                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        FileLogger.ApplicationLog(nameof(FetchHistoryFromApiAsync),
                            $"API failed: {(int)response.StatusCode} - {response.ReasonPhrase}");
                        return TryLoadFromCache(response.ReasonPhrase);
                    }

                    var result = JsonConvert.DeserializeObject<HistoryResponse>(responseString);

                    if (result == null || !result.IsSuccess || result.Data == null)
                    {
                        FileLogger.ApplicationLog(nameof(FetchHistoryFromApiAsync),
                            "Invalid response structure.");
                        return TryLoadFromCache("Invalid response structure");
                    }

                    var newApiData = result.Data;
                    var localCache = GetStoredHistory() ?? new List<HistoryModel>();

                    localCache.RemoveAll(x => x.CreatedOn.Date >= fromDate.Date && x.CreatedOn.Date <= toDate.Date);

                    localCache.AddRange(newApiData);

                    localCache = localCache.OrderByDescending(x => x.CreatedOn).ToList();

                    try
                    {
                        SaveStoredHistory(localCache);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(FetchHistoryFromApiAsync), $"SaveStoredHistory failed: {ex.Message}");
                    }

                    return new HistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = false,
                        Data = localCache
                    };
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(FetchHistoryFromApiAsync),
                    $"Exception: {ex.Message}");

                return TryLoadFromCache(ex.Message);
            }
        }

        private HistoryFetchResult TryLoadFromCache(string errorMessage)
        {
            try
            {
                var cached = GetStoredHistory();

                if (cached != null && cached.Any())
                {
                    FileLogger.ApplicationLog(nameof(TryLoadFromCache),
                        "Loaded history from local cache.");

                    return new HistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = true,
                        ErrorMessage = errorMessage,
                        Data = cached
                    };
                }

                FileLogger.ApplicationLog(nameof(TryLoadFromCache),
                    "No local history cache available.");

                return new HistoryFetchResult
                {
                    IsSuccess = false,
                    IsFromCache = false,
                    ErrorMessage = errorMessage,
                    Data = new List<HistoryModel>()
                };
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(TryLoadFromCache),
                    $"Cache load failed: {ex.Message}");

                return new HistoryFetchResult
                {
                    IsSuccess = false,
                    IsFromCache = false,
                    ErrorMessage = ex.Message,
                    Data = new List<HistoryModel>()
                };
            }
        }

        public async Task<PositionHistoryFetchResult> FetchPositionHistoryFromApiAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var payload = new
                {
                    clientID = _sessionService.UserId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService
                    .PostRawAsync(AppConfig.GetPositionHistoryForClient.ToReplaceUrl(_sessionService.PrimaryDomain), content)
                    .ConfigureAwait(false))
                {
                    if (response == null || response.Content == null)
                    {
                        FileLogger.ApplicationLog(nameof(FetchPositionHistoryFromApiAsync),
                            "Null response from PositionHistory API.");

                        return TryLoadPositionHistoryFromCache("Null response from API");
                    }

                    var responseString = await response.Content.ReadAsStringAsync()
                        .ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        FileLogger.ApplicationLog(nameof(FetchPositionHistoryFromApiAsync),
                            $"API failed: {(int)response.StatusCode} - {response.ReasonPhrase}");

                        return TryLoadPositionHistoryFromCache(response.ReasonPhrase);
                    }

                    var result = JsonConvert.DeserializeObject<PositionHistoryResponse>(responseString);

                    if (result == null || !result.IsSuccess || result.Data == null)
                    {
                        FileLogger.ApplicationLog(nameof(FetchPositionHistoryFromApiAsync),
                            "Invalid response structure.");

                        return TryLoadPositionHistoryFromCache("Invalid response structure");
                    }

                    var newApiData = result.Data;
                    var localCache = GetStoredPositionHistory() ?? new List<PositionHistoryModel>();

                    localCache.RemoveAll(x => x.UpdatedAt.Date >= fromDate.Date && x.UpdatedAt.Date <= toDate.Date);
                    localCache.AddRange(newApiData);
                    localCache = localCache.OrderByDescending(x => x.UpdatedAt).ToList();

                    try
                    {
                        SaveStoredPositionHistory(localCache);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(FetchPositionHistoryFromApiAsync), $"SaveStoredPositionHistory failed: {ex.Message}");
                    }

                    return new PositionHistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = false,
                        Data = localCache
                    };
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(FetchPositionHistoryFromApiAsync),
                    $"Exception: {ex.Message}");

                return TryLoadPositionHistoryFromCache(ex.Message);
            }
        }

        private PositionHistoryFetchResult TryLoadPositionHistoryFromCache(string errorMessage)
        {
            try
            {
                var cached = GetStoredPositionHistory();

                if (cached != null && cached.Any())
                {
                    FileLogger.ApplicationLog("TryLoadPositionHistoryFromCache",
                        "Loaded position history from local cache.");

                    return new PositionHistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = true,
                        ErrorMessage = errorMessage,
                        Data = cached
                    };
                }

                FileLogger.ApplicationLog("TryLoadPositionHistoryFromCache",
                    "No local position history cache available.");

                return new PositionHistoryFetchResult
                {
                    IsSuccess = false,
                    IsFromCache = false,
                    ErrorMessage = errorMessage,
                    Data = new List<PositionHistoryModel>()
                };
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog("TryLoadPositionHistoryFromCache",
                    $"Cache load failed: {ex.Message}");

                return new PositionHistoryFetchResult
                {
                    IsSuccess = false,
                    IsFromCache = false,
                    ErrorMessage = ex.Message,
                    Data = new List<PositionHistoryModel>()
                };
            }
        }

        #endregion

    }
}