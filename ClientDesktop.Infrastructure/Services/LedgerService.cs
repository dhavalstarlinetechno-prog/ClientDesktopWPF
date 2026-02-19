using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using System.Windows;

namespace ClientDesktop.Infrastructure.Services
{
    public class LedgerService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<Ledgermodel>> _symbolMode;

        public LedgerService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        public async Task<(bool Success, string ErrorMessage, LedgerAuthData ResponseData)> VerifyUserPasswordAsync(string clientId, string password, string licenseId)
        {
            try
            {
                // ✅ Payload
                var payload = new
                {
                    type = "USER",
                    password,
                    clientId
                };

                // ✅ Resolve server URL
                var serverDomain = _sessionService.ServerListData
                    .FirstOrDefault(x => x.licenseId.ToString() == licenseId)
                    ?.primaryDomain;

                var baseUrl = CommonHelper.ToReplaceUrl(AppConfig.LedgerAuthnticationURL, _sessionService.PrimaryDomain);
                var fullUrl = baseUrl.Contains("http")
                    ? baseUrl
                    : $"{serverDomain}{baseUrl}";

                // ✅ Call ApiService
                var result = await _apiService
                    .PutAsync<LedgerAuthResponse>(fullUrl, payload);

                if (result == null)
                    return (false, CommonMessages.InvalidResponse, null);

                if (!result.isSuccess || result.data == null)
                    return (false,
                        result.successMessage ?? CommonMessages.FailedVerifyUser,
                        null);

                // ✅ Interpret response message
                var message = result.data.msg?.FirstOrDefault()?.Trim() ?? string.Empty;

                if (message.Equals(CommonMessages.WrongPassword, StringComparison.OrdinalIgnoreCase))
                    return (false, CommonMessages.WrongPassword, result.data);

                if (message.Contains("maximum", StringComparison.OrdinalIgnoreCase))
                    return (false, CommonMessages.MaximumPasswordAttempts, result.data);

                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return (false, CommonMessages.UserNotFound, result.data);

                if (message.Equals("OK !", StringComparison.OrdinalIgnoreCase))
                    return (true, null, result.data);

                return (false, message, result.data);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<LedgerUserDetail> GetLedgerUserDetail()
        {
            try
            {
                string baseUrl = CommonHelper.ToReplaceUrl(AppConfig.LederUserURL, _sessionService.PrimaryDomain);
                string url = $"{baseUrl}{_sessionService.UserId}";

                var response = await _apiService.GetAsync<LedgerUserResponse>(url);

                if (response == null)
                {
                    MessageBox.Show("Invalid response from server",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    return null;
                }

                return response.data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return null;
            }
        }

        public async Task<(bool Success, string ErrorMessage, LedgerData ResponseData)> GetLedgerListAsync(string clientId, DateTime fromDate, DateTime toDate, string licenseId)
        {
            try
            {
                var payload = new
                {
                    userName = clientId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                // Resolve server domain
                var serverDomain = _sessionService.ServerListData
                    .FirstOrDefault(w => w.licenseId.ToString() == licenseId)
                    ?.primaryDomain;

                if (string.IsNullOrEmpty(serverDomain))
                    return (false, "Invalid server domain.", null);

                var baseUrl = CommonHelper.ToReplaceUrl(AppConfig.GetLedgerListURL, _sessionService.PrimaryDomain);

                var fullUrl = baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? baseUrl
                    : $"{serverDomain}{baseUrl}";

                var result = await _apiService.PutAsync<LedgerResponse>(fullUrl, payload);

                if (result == null)
                    return (false, CommonMessages.InvalidResponse, null);

                if (!result.isSuccess || result.data == null)
                    return (false,
                        result.successMessage ?? CommonMessages.FailedVerifyUser,
                        null);

                return (true, null, result.data);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }


    }
}
