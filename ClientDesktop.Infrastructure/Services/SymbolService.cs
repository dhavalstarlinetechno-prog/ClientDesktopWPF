using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClientDesktop.Infrastructure.Services
{
    public class SymbolService
    {
        #region Variables

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<Symbolmodel>> _symbolModel;

        #endregion Variables

        #region Constructor
        public SymbolService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;

        }

        #endregion Constructor

        #region Methods
        public async Task<Symbolmodel> GetSymbolsAsync()
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(
                    AppConfig.symbolstreeviewwithclientrights,
                    _sessionService.PrimaryDomain);

                var jsonResponse = await _apiService.GetAsync<object>(url);

                if (jsonResponse == null)
                {                   
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {                   
                    return null;
                }

                
                var folderList = JsonConvert.DeserializeObject<List<Folder>>(
                    parsed.data.ToString());

                return new Symbolmodel
                {
                    Data = folderList
                };
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetSymbolsAsync), $"Request failed: {ex.Message}");
                return null;
            }
        }
        public async Task<SubSymbolRoot> GetSubSymbolsAsync()
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(
                    AppConfig.getsymbolsbyrouteforclient,
                    _sessionService.PrimaryDomain);

                var jsonResponse = await _apiService.GetAsync<object>(url);

                if (jsonResponse == null)
                {                   
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {                   
                    return null;
                }
              
                var dataList = JsonConvert.DeserializeObject<List<SubSymbolModel>>(
                    parsed.data.ToString());

                return new SubSymbolRoot
                {
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetSubSymbolsAsync), $"Request failed: {ex.Message}");
                return null;
            }
        }
        public async Task<SubSymbolRoot> Getsymbolsbyrouteforclient(int routeId)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.NodeTree + routeId, _sessionService.PrimaryDomain);

                var jsonResponse = await _apiService.GetAsync<object>(url);

                if (jsonResponse == null)
                {                   
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {                    
                    return null;
                }
               
                var dataList = JsonConvert.DeserializeObject<List<SubSymbolModel>>(
                    parsed.data.ToString());

                return new SubSymbolRoot
                {
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Getsymbolsbyrouteforclient), $"Request failed: {ex.Message}");
                return null;
            }
        }
        public async Task<SubSymbolRoot> GetDolorSignTree(string symbolId)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.DolorsignSymbol + symbolId, _sessionService.PrimaryDomain);

                var jsonResponse = await _apiService.GetAsync<object>(url);

                if (jsonResponse == null)
                {                    
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {                    
                    return null;
                }
               
                var dataList = JsonConvert.DeserializeObject<List<SubSymbolModel>>(
                    parsed.data.ToString());

                return new SubSymbolRoot
                {
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetDolorSignTree), $"Request failed: {ex.Message}");
                return null;
            }
        }
        public async Task<SymbolModel?> GetSymbolDetailsAsync(int symbolId)
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
                FileLogger.ApplicationLog(nameof(GetSymbolDetailsAsync), "Error fetching symbol: " + ex.Message);
                return null;
            }
        }

        #endregion Methods
    }
}
