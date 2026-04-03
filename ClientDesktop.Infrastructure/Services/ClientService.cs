using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for managing client details, fetching them from the API, and handling local caching.
    /// </summary>
    public class ClientService
    {
        #region Fields

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<ClientDetails>> _clientRepo;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ClientService class.
        /// </summary>
        public ClientService(IApiService apiService, SessionService sessionService)
        {
            try
            {
                _apiService = apiService;
                _sessionService = sessionService;
                _clientRepo = new FileRepository<List<ClientDetails>>();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ClientService), ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves the master list of clients, optionally updating specific details, and caches the result locally.
        /// </summary>
        public async Task<(bool Success, string ErrorMessage, List<ClientDetails> Clients, bool IsViewLocked)> GetClientListAsync(ClientDetails clientDetails)
        {
            var cachedData = new List<ClientDetails>();
            bool isViewLocked = false;

            try
            {
                string folderName = AESHelper.ToBase64UrlSafe(_sessionService.LicenseId);
                string fileName = AESHelper.ToBase64UrlSafe(_sessionService.UserId);
                string relativePath = System.IO.Path.Combine(folderName, fileName);

                var loadedData = _clientRepo.Load(relativePath, "client");
                if (loadedData != null)
                {
                    cachedData = loadedData;
                }

                string url = CommonHelper.ToReplaceUrl(AppConfig.MasterClientListURL, _sessionService.PrimaryDomain);
                var responseData = await _apiService.GetAsync<ClientDetailsRootModel>(url);

                if (responseData == null || !responseData.isSuccess)
                {
                    FileLogger.ApplicationLog(nameof(GetClientListAsync), "Failed to get client details from API. Falling back to cached data.");
                    return (true, "Failed to get client details", cachedData, false);
                }

                if (responseData.data == null)
                {
                    FileLogger.ApplicationLog(nameof(GetClientListAsync), "Invalid or empty response from API. Falling back to cached data.");
                    return (true, "Invalid or empty response", cachedData, false);
                }

                var clientObj = responseData.data;

                if (clientDetails != null)
                {
                    clientObj.CreditAmount = clientDetails.CreditAmount;
                    clientObj.UplineAmount = clientDetails.UplineAmount;
                    clientObj.Balance = clientDetails.Balance;
                    clientObj.OccupiedMarginAmount = clientDetails.OccupiedMarginAmount;
                    clientObj.UplineCommission = clientDetails.UplineCommission;
                }

                isViewLocked = clientObj.IsViewLocked;

                var listToSave = new List<ClientDetails> { clientObj };

                _clientRepo.Save(relativePath, listToSave, "client");

                return (true, null, listToSave, isViewLocked);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetClientListAsync), ex);
                return (true, ex.Message, cachedData, false);
            }
        }

        /// <summary>
        /// Retrieves the specific details for the currently authenticated client.
        /// </summary>
        public async Task<(bool Success, string ErrorMessage, ClientDetails Clients)> GetSpecificClientListAsync()
        {
            try
            {
                string url = $"{CommonHelper.ToReplaceUrl(AppConfig.ClientListURL, _sessionService.PrimaryDomain)}/{_sessionService.UserId}";
                var responseData = await _apiService.GetAsync<ClientDetailsRootModel>(url);

                if (responseData == null || responseData.data == null)
                {
                    FileLogger.ApplicationLog(nameof(GetSpecificClientListAsync), "Failed to get specific client details from API.");
                    return (true, "Failed to get specific client details", null);
                }

                return (true, null, responseData.data);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetSpecificClientListAsync), ex);
                return (true, ex.Message, null);
            }
        }

        #endregion
    }
}