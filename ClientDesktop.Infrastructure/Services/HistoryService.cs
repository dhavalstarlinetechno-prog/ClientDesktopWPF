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
        private readonly IRepository<List<HistoryModel>> _historyRepo;
        private readonly IRepository<List<PositionHistoryModel>> _positionHistoryRepo;

        public HistoryService()
        {
            _apiService = new ApiService();
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
            string domain = SessionManager.ServerListData
                .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?
                .serverDisplayName;

            return Path.Combine(
                AESHelper.ToBase64UrlSafe(domain),
                AESHelper.ToBase64UrlSafe(SessionManager.UserId)
            );
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

        public async Task<HistoryFetchResult> FetchHistoryFromApiAsync(
    DateTime fromDate, DateTime toDate)
        {
            try
            {
                var dealer = SessionManager.ClientListData?.FirstOrDefault();
                if (dealer == null)
                {
                    FileLogger.ApplicationLog("HistoryService", "FetchHistoryFromApiAsync", "DealerId not found.");
                    return TryLoadFromCache("DealerId missing");
                }

                var payload = new
                {
                    clientID = SessionManager.UserId,
                    dealerID = dealer.DealerId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService
                    .PostRawAsync(AppConfig.GetHistoryForClient.ToReplaceUrl(), content))
                {
                    if (response == null || response.Content == null)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchHistoryFromApiAsync", "Null response from API.");
                        return TryLoadFromCache("Null response from API");
                    }

                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchHistoryFromApiAsync",
                            $"API failed: {(int)response.StatusCode} - {response.ReasonPhrase}");
                        return TryLoadFromCache(response.ReasonPhrase);
                    }

                    var result = JsonConvert.DeserializeObject<HistoryResponse>(responseString);

                    if (result == null || !result.IsSuccess || result.Data == null)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchHistoryFromApiAsync",
                            "Invalid response structure.");
                        return TryLoadFromCache("Invalid response structure");
                    }

                    try
                    {
                        SaveStoredHistory(result.Data);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchHistoryFromApiAsync",
                            $"SaveStoredHistory failed: {ex.Message}");
                    }

                    return new HistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = false,
                        Data = result.Data
                    };
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog("HistoryService", "FetchHistoryFromApiAsync",
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
                    FileLogger.ApplicationLog("HistoryService", "TryLoadFromCache",
                        "Loaded history from local cache.");

                    return new HistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = true,
                        ErrorMessage = errorMessage,
                        Data = cached
                    };
                }

                FileLogger.ApplicationLog("HistoryService", "TryLoadFromCache",
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
                FileLogger.ApplicationLog("HistoryService", "TryLoadFromCache",
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

        public async Task<PositionHistoryFetchResult> FetchPositionHistoryFromApiAsync(
    DateTime fromDate, DateTime toDate)
        {
            try
            {
                var payload = new
                {
                    clientID = SessionManager.UserId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService
                    .PostRawAsync(AppConfig.GetPositionHistoryForClient.ToReplaceUrl(), content)
                    .ConfigureAwait(false))
                {
                    if (response == null || response.Content == null)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchPositionHistoryFromApiAsync",
                            "Null response from PositionHistory API.");

                        return TryLoadPositionHistoryFromCache("Null response from API");
                    }

                    var responseString = await response.Content.ReadAsStringAsync()
                        .ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchPositionHistoryFromApiAsync",
                            $"API failed: {(int)response.StatusCode} - {response.ReasonPhrase}");

                        return TryLoadPositionHistoryFromCache(response.ReasonPhrase);
                    }

                    var result = JsonConvert.DeserializeObject<PositionHistoryResponse>(responseString);

                    if (result == null || !result.IsSuccess || result.Data == null)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchPositionHistoryFromApiAsync",
                            "Invalid response structure.");

                        return TryLoadPositionHistoryFromCache("Invalid response structure");
                    }

                    try
                    {
                        SaveStoredPositionHistory(result.Data);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog("HistoryService", "FetchPositionHistoryFromApiAsync",
                            $"SaveStoredPositionHistory failed: {ex.Message}");
                    }

                    return new PositionHistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = false,
                        Data = result.Data
                    };
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog("HistoryService", "FetchPositionHistoryFromApiAsync",
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
                    FileLogger.ApplicationLog("HistoryService", "TryLoadPositionHistoryFromCache",
                        "Loaded position history from local cache.");

                    return new PositionHistoryFetchResult
                    {
                        IsSuccess = true,
                        IsFromCache = true,
                        ErrorMessage = errorMessage,
                        Data = cached
                    };
                }

                FileLogger.ApplicationLog("HistoryService", "TryLoadPositionHistoryFromCache",
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
                FileLogger.ApplicationLog("HistoryService", "TryLoadPositionHistoryFromCache",
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