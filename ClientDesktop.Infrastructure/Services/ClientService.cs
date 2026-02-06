using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;

namespace ClientDesktop.Infrastructure.Services
{
    public class ClientService
    {
        private readonly IApiService _apiService;
        private readonly IRepository<List<ClientDetails>> _clientRepo;

        public ClientService()
        {
            _apiService = new ApiService();
            _clientRepo = new FileRepository<List<ClientDetails>>();
        }

        public async Task<(bool Success, string ErrorMessage, List<ClientDetails> Clients, bool IsViewLocked)> GetClientListAsync(ClientDetails clientDetails)
        {
            string folderName = AESHelper.ToBase64UrlSafe(SessionManager.LicenseId);
            string fileName = AESHelper.ToBase64UrlSafe(SessionManager.UserId);
            string relativePath = System.IO.Path.Combine(folderName, fileName);

            var cachedData = new List<ClientDetails>();
            bool isViewLocked = false;

            var loadedData = _clientRepo.Load(relativePath, "client");
            if (loadedData != null)
            {
                cachedData = loadedData;
            }

            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.MasterClientListURL);
                var responseData = await _apiService.GetAsync<ClientDetailsRootModel>(url);

                if (responseData == null || !responseData.isSuccess)
                {
                    return (true, "Failed to get client details", cachedData, false);
                }

                if (responseData.data == null)
                {
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
                return (true, ex.Message, cachedData, false);
            }
        }

        public async Task<(bool Success, string ErrorMessage, ClientDetails Clients)> GetSpecificClientListAsync()
        {
            try
            {
                string url = $"{CommonHelper.ToReplaceUrl(AppConfig.ClientListURL)}/{SessionManager.UserId}";
                var responseData = await _apiService.GetAsync<ClientDetailsRootModel>(url);

                if (responseData == null || responseData.data == null)
                {
                    return (true, "Failed to get specific client details", null);
                }

                return (true, null, responseData.data);
            }
            catch (Exception ex)
            {
                return (true, ex.Message, null);
            }
        }
    }
}
