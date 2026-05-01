using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Services
{
    public class BanScriptService
    {
        #region Fields

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<BanScriptResponse> _banscriptRepo;

        #endregion Fields

        #region Constructor
        public BanScriptService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
            _banscriptRepo = new FileRepository<BanScriptResponse>();
        }

        #endregion Constructor

        #region Methods
        public async Task<BanScriptResponse> GetBanScript(bool forceApiSync = false)
        {
            try
            {
                string folderName = AESHelper.ToBase64UrlSafe(_sessionService.LicenseId);
                string fileName = AESHelper.ToBase64UrlSafe(_sessionService.UserId);
                string relativePath = Path.Combine(folderName, fileName);

                var cachedData = _banscriptRepo.Load(relativePath, "banscript");

                if (!forceApiSync && cachedData != null)
                {
                    return cachedData;
                }

                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    DateTime currentDate = DateTime.Today;
                    string date = currentDate.ToString("yyyy-MM-dd");
                    string url = CommonHelper.ToReplaceUrl(AppConfig.GetBanscript + date, _sessionService.PrimaryDomain);
                    var apiData = await _apiService.GetAsync<BanScriptResponse>(url);

                    if (apiData != null)
                    {
                        _= Task.Run(() => _banscriptRepo.Save(relativePath, apiData, "banscript"));
                        return apiData;
                    }
                }
                else
                {
                    FileLogger.ApplicationLog(nameof(GetBanScript), "No Internet Connection. Loading Local Data.");
                }

                return cachedData ?? new BanScriptResponse();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetBanScript) + ex.Message);
                return new BanScriptResponse();
            }
        }

        #endregion Methods

    }
}
