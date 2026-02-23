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
    public class SymbolSpecificationService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;

        public SymbolSpecificationService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;

        }
        //public async Task<SymbolModel> GetSymbolAsync(int symbolId)
        //{
        //    try
        //    {
        //        string url = CommonHelper.ToReplaceUrl(
        //            AppConfig.SpectificationURL + symbolId,
        //            _sessionService.PrimaryDomain);

        //        var response = await _apiService.GetAsync<ApiResponse<SymbolModel>>(url);

        //        if (response == null || response.Data == null)
        //        {
        //            MessageBox.Show("No data received.");
        //            return new SymbolModel();
        //        }

        //        return response.Data;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error fetching symbol: " + ex.Message);
        //        return new SymbolModel();
        //    }
        //}

        public async Task<SymbolModel?> GetSymbolAsync(int symbolId)
        {
            try
            {
                if (symbolId <= 0)
                    return null;

                string url = CommonHelper.ToReplaceUrl(
                    AppConfig.SpectificationURL + symbolId,
                    _sessionService.PrimaryDomain);

                var response = await _apiService.GetAsync<ApiResponse<SymbolModel>>(url);

                if (response?.Data == null)
                {
                    MessageBox.Show("No data received.");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching symbol: " + ex.Message);
                return null;
            }
        }

    }
}
