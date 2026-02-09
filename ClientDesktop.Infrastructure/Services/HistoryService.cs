using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using Newtonsoft.Json;
using System.Text;

namespace ClientDesktop.Infrastructure.Services
{
    public class HistoryService
    {
        private readonly IApiService _apiService;

        public HistoryService()
        {
            _apiService = new ApiService();
        }

        #region Core Data Loading Logic (Cache + API with Fallback)

        /// <summary>
        /// Loads History (Deals/Orders). 
        /// Strategy: Always load local cache first. If API is needed but fails, return local cache.
        /// </summary>
        public async Task<List<HistoryModel>> GetDealsOrOrdersDataAsync(string userId, string licenseId, string domain)
        {
            // 1. Path Calculation
            string filePath = GetUserFilePath(domain, userId);

            // 2. Load from Cache (IMMEDIATE FALLBACK)
            // We load this FIRST. If API fails later, this variable holds the data to be returned.
            List<HistoryModel> historyList = null;
            try
            {
                historyList = CommonHelper.LoadHistoryDataFromCache(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cache Load Error: " + ex.Message);
            }

            // Ensure list is never null
            if (historyList == null) historyList = new List<HistoryModel>();

            try
            {
                // 3. Logic to determine if we need to fetch from API
                bool needFetch = false;
                DateTime fromDate = (licenseId == "1") ? new DateTime(2025, 6, 1) : new DateTime(1970, 1, 1);
                DateTime toDate = DateTime.Today;

                if (historyList.Count == 0)
                {
                    // Cache is empty, force fetch
                    needFetch = true;
                }
                else
                {
                    // Incremental update check
                    var lastDate = historyList.Max(h => h.createdOn);
                    if (lastDate.Date <= DateTime.Today)
                    {
                        fromDate = lastDate;
                        toDate = DateTime.Today.AddDays(1);
                        needFetch = true;
                    }
                }

                // 4. Fetch from API if needed
                if (needFetch)
                {
                    var dealerId = SessionManager.ClientListData.FirstOrDefault()?.DealerId;

                    var (success, error, apiData) = await FetchHistoryFromApiAsync(userId, dealerId, fromDate, toDate, licenseId);

                    if (success && apiData != null && apiData.Count > 0)
                    {
                        // Remove overlaps and add new data
                        var dataToRemove = historyList.Where(h => h.createdOn >= fromDate && h.createdOn <= toDate).ToList();
                        foreach (var item in dataToRemove) historyList.Remove(item);

                        historyList.AddRange(apiData);

                        // Save back to Cache (Update local storage)
                        await SaveHistoryDataToCacheAsync(filePath, historyList);
                    }
                    else
                    {
                        // API FAILED: We intentionally do nothing here.
                        // 'historyList' still contains the data loaded from cache at step 2.
                        // This ensures the grid shows whatever local data we have.
                        Console.WriteLine($"API Fetch failed: {error}. Returning cached data.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Safety net: If logic crashes, log it and return whatever we managed to load from cache
                Console.WriteLine("Error in GetDealsOrOrdersDataAsync: " + ex.Message);
            }

            return historyList;
        }

        /// <summary>
        /// Loads Position History.
        /// Strategy: Always load local cache first. If API is needed but fails, return local cache.
        /// </summary>
        public async Task<List<PositionHistoryModel>> GetPositionHistoryDataAsync(string userId, string licenseId, string domain)
        {
            // 1. Path Calculation
            string filePath = GetUserFilePath(domain, userId);

            // 2. Load from Cache (IMMEDIATE FALLBACK)
            List<PositionHistoryModel> posList = null;
            try
            {
                posList = CommonHelper.LoadPositionHistoryDataFromCache(filePath);
            }
            catch
            {
                // Ignore cache errors
            }

            if (posList == null) posList = new List<PositionHistoryModel>();

            try
            {
                // 3. Logic to determine if we need to call API
                bool needFetch = false;
                DateTime fromDate = (licenseId == "1") ? new DateTime(2025, 6, 1) : new DateTime(1970, 1, 1);
                DateTime toDate = DateTime.Today;

                if (posList.Count == 0)
                {
                    needFetch = true;
                }
                else
                {
                    var lastDate = posList.Max(h => h.UpdatedAt);
                    if (lastDate.Date <= DateTime.Today)
                    {
                        fromDate = lastDate;
                        toDate = DateTime.Today.AddDays(1);
                        needFetch = true;
                    }
                }

                // 4. Fetch from API if needed
                if (needFetch)
                {
                    var (success, error, apiData) = await FetchPositionHistoryFromApiAsync(userId, fromDate, toDate, licenseId);

                    if (success && apiData != null && apiData.Count > 0)
                    {
                        // Clean overlaps for open positions or updated records
                        var dataToRemove = posList.Where(h => h.LastOutAt == null || (h.UpdatedAt >= fromDate && h.UpdatedAt <= toDate)).ToList();
                        foreach (var item in dataToRemove) posList.Remove(item);

                        posList.AddRange(apiData);

                        // Save Cache
                        await SavePositionHistoryDataToCacheAsync(filePath, posList);
                    }
                    else
                    {
                        // API FAILED: Fallback to existing cache
                        Console.WriteLine($"API Position Fetch failed: {error}. Returning cached data.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetPositionHistoryDataAsync: " + ex.Message);
            }

            return posList;
        }

        #endregion

        #region Private Helpers (File & Path)

        private string GetUserFilePath(string domain, string userId)
        {
            return Path.Combine(
                Path.Combine(AppConfig.dataFolder, AESHelper.ToBase64UrlSafe(domain)),
                $"{AESHelper.ToBase64UrlSafe(userId)}.dat"
            );
        }

        private async Task SaveHistoryDataToCacheAsync(string filePath, List<HistoryModel> historyList)
        {
            await Task.Run(() =>
            {
                try
                {
                    var existingData = File.Exists(filePath)
                        ? JsonConvert.DeserializeObject<Dictionary<string, object>>(AESHelper.DecompressAndDecryptString(File.ReadAllText(filePath)))
                        : new Dictionary<string, object>();

                    existingData["History"] = historyList;
                    string updatedJson = JsonConvert.SerializeObject(existingData);
                    string encryptedUpdatedJson = AESHelper.CompressAndEncryptString(updatedJson);

                    string folder = Path.GetDirectoryName(filePath);
                    CommonHelper.SaveEncryptedData(folder, AESHelper.ToBase64UrlSafe(SessionManager.UserId), encryptedUpdatedJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving history cache: " + ex.Message);
                }
            });
        }

        private async Task SavePositionHistoryDataToCacheAsync(string filePath, List<PositionHistoryModel> positionHistoryList)
        {
            await Task.Run(() =>
            {
                try
                {
                    var existingData = File.Exists(filePath)
                        ? JsonConvert.DeserializeObject<Dictionary<string, object>>(AESHelper.DecompressAndDecryptString(File.ReadAllText(filePath)))
                        : new Dictionary<string, object>();

                    existingData["PositionHistory"] = positionHistoryList;
                    string updatedJson = JsonConvert.SerializeObject(existingData);
                    string encryptedUpdatedJson = AESHelper.CompressAndEncryptString(updatedJson);

                    string folder = Path.GetDirectoryName(filePath);
                    CommonHelper.SaveEncryptedData(folder, AESHelper.ToBase64UrlSafe(SessionManager.UserId), encryptedUpdatedJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving position cache: " + ex.Message);
                }
            });
        }

        #endregion

        #region API Calls

        public async Task<(bool Success, string ErrorMessage, List<HistoryModel> ResponseData)> FetchHistoryFromApiAsync(
             string clientId, string dealerId, DateTime fromDate, DateTime toDate, string licenseId)
        {
            try
            {
                var payload = new
                {
                    clientID = clientId,
                    dealerID = dealerId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService.PostRawAsync(AppConfig.GetHistoryForClient.ToReplaceUrl(), content))
                {
                    // ✅ FIX: Check if Content is null before accessing it
                    if (response.Content == null)
                    {
                        return (false, $"{(int)response.StatusCode}: {response.ReasonPhrase}", null);
                    }

                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = JsonConvert.DeserializeObject<dynamic>(responseString);
                        return (false, error?.exception?.message?.ToString() ??
                                $"{(int)response.StatusCode}: {response.ReasonPhrase}", null);
                    }

                    var result = JsonConvert.DeserializeObject<HistoryResponse>(responseString);

                    if (result == null) return (false, "Invalid response from server", null);
                    if (!result.isSuccess || result.data == null)
                        return (false, result.successMessage ?? "Failed to retrieve history data", null);

                    return (true, null, result.data);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string ErrorMessage, List<PositionHistoryModel> ResponseData)> FetchPositionHistoryFromApiAsync(
             string clientId, DateTime fromDate, DateTime toDate, string licenseId)
        {
            try
            {
                var payload = new
                {
                    clientID = clientId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService.PostRawAsync(AppConfig.GetPositionHistoryForClient.ToReplaceUrl(), content).ConfigureAwait(false))
                {
                    // ✅ FIX: Check if Content is null here too
                    if (response.Content == null)
                    {
                        return (false, $"{(int)response.StatusCode}: {response.ReasonPhrase}", null);
                    }

                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = JsonConvert.DeserializeObject<dynamic>(responseString);
                        return (false, error?.exception?.message?.ToString() ??
                                $"{(int)response.StatusCode}: {response.ReasonPhrase}", null);
                    }

                    var result = JsonConvert.DeserializeObject<PositionHistoryResponse>(responseString);

                    if (result == null) return (false, "Invalid response from server", null);
                    if (!result.IsSuccess || result.Data == null)
                        return (false, result.SuccessMessage ?? "Failed to retrieve position history data", null);

                    return (true, null, result.Data);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        #endregion
    }
}