using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClientDesktop.Infrastructure.Services
{
    public class BanScriptService
    {
        #region Fields

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<BanScripts>> _banscriptRepo;

        #endregion Fields

        #region Constructor
        public BanScriptService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        #endregion Constructor

        #region Methods
        public async Task<BanScriptResponse> GetBanScript()
        {
            try
            {
                DateTime currentDate = DateTime.Today;
                string date = currentDate.ToString("yyyy-MM-dd");
                string url = CommonHelper.ToReplaceUrl(AppConfig.GetBanscript + date, _sessionService.PrimaryDomain);
               
                var result = await _apiService.GetAsync<BanScriptResponse>(url);
                return result ?? new BanScriptResponse();
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
