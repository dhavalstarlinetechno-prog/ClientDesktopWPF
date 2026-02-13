using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
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

        public async Task<(bool Success, string ErrorMessage, List<HistoryModel> ResponseData)> FetchHistoryFromApiAsync(
              DateTime fromDate, DateTime toDate)
        {
            try
            {
                var payload = new
                {
                    clientID = SessionManager.UserId,
                    dealerID = SessionManager.ClientListData.FirstOrDefault().DealerId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService.PostRawAsync(AppConfig.GetHistoryForClient.ToReplaceUrl(), content))
                {
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
                    if (!result.IsSuccess || result.Data == null)
                        return (false, result.SuccessMessage ?? "Failed to retrieve history data", null);

                    SaveStoredHistory(result.Data);
                    return (true, null, result.Data);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string ErrorMessage, List<PositionHistoryModel> ResponseData)> FetchPositionHistoryFromApiAsync(
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

                    SaveStoredPositionHistory(result.Data);
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