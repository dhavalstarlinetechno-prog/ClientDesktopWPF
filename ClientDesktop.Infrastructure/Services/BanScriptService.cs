using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
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
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<BanScripts>> _banscriptRepo;

        public BanScriptService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }
        public async Task<BanScriptResponse> GetBanScript()
        {
            try
            {
                DateTime currentDate = DateTime.Today;
                string date = currentDate.ToString("yyyy-MM-dd");
                string url = CommonHelper.ToReplaceUrl(AppConfig.GetBanscript + date, _sessionService.PrimaryDomain);

                // Deserialize direct Model ma thase
                var result = await _apiService.GetAsync<BanScriptResponse>(url);
                return result ?? new BanScriptResponse();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching ban script: " + ex.Message);
                return new BanScriptResponse();
            }
        }

    }
}
