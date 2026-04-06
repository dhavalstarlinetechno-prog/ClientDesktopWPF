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
    public class SymbolSpecificationService
    {
        #region Variables

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;

        #endregion Variables

        #region Constructor
        public SymbolSpecificationService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;

        }
        #endregion Constructor

        #region Methods
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
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetSymbolAsync) + ex.Message);
                return null;
            }
        }

        #endregion Methods

    }
}
