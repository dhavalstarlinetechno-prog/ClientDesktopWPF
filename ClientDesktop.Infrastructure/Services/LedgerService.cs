using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClientDesktop.Infrastructure.Services
{
    public class LedgerService
    {
        private readonly IApiService _apiService;
        private readonly IRepository<List<Ledgermodel>> _symbolMode;

        public LedgerService()
        {
            _apiService = new ApiService();
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
                var serverDomain = SessionManager.ServerListData
                    .FirstOrDefault(x => x.licenseId.ToString() == licenseId)
                    ?.primaryDomain;

                var baseUrl = CommonHelper.ToReplaceUrl(AppConfig.LedgerAuthnticationURL);
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
                string baseUrl = CommonHelper.ToReplaceUrl(AppConfig.LederUserURL);
                string url = $"{baseUrl}{SessionManager.UserId}";

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
                var serverDomain = SessionManager.ServerListData
                    .FirstOrDefault(w => w.licenseId.ToString() == licenseId)
                    ?.primaryDomain;

                if (string.IsNullOrEmpty(serverDomain))
                    return (false, "Invalid server domain.", null);

                var baseUrl = CommonHelper.ToReplaceUrl(AppConfig.GetLedgerListURL);

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
