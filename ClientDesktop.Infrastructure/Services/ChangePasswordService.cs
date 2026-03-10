using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Provides functionality to verify and update a user's password by communicating with an external API.
    /// </summary>
    /// <remarks>This service requires a valid session and interacts with an external API to change the user's
    /// password. It handles various error messages returned from the API, including cases for incorrect passwords and
    /// user not found scenarios.</remarks>
    public class ChangePasswordService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;

        public ChangePasswordService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        /// <summary>
        /// Verifies and updates the user's password via API.
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> VerifyUserPasswordAsync(string userName, string oldPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var payload = new
                {
                    userName = userName,
                    oldUserPassword = oldPassword,
                    userPassword = newPassword,
                    userConfirmPassword = confirmPassword
                };

                string url = CommonHelper.ToReplaceUrl(AppConfig.ChangePasswordURL, _sessionService.PrimaryDomain);
                var result = await _apiService.PutAsync<dynamic>(url, payload);

                if (result == null)
                    return (false, "Invalid Response");

                bool isSuccess = result.isSuccess ?? false;

                if (!isSuccess || result.data == null)
                    return (false, result.successMessage?.ToString() ?? "Failed to verify user.");

                string message = string.Empty;
                if (result.data.msg != null && result.data.msg.Count > 0)
                {
                    message = result.data.msg[0].ToString().Trim();
                }

                if (message.Equals("Wrong password", StringComparison.OrdinalIgnoreCase))
                    return (false, "Wrong Password");

                if (message.Contains("maximum"))
                    return (false, "Maximum password attempts reached.");

                if (message.Contains("not found"))
                    return (false, "User not found.");

                if (message.Equals("OK !", StringComparison.OrdinalIgnoreCase) || isSuccess)
                    return (true, null);

                return (false, message);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(VerifyUserPasswordAsync), ex);
                return (false, ex.Message);
            }
        }
    }
}