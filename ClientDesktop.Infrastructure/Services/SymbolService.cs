using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
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
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<Symbolmodel>> _symbolModel;

        public SymbolService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;

        }

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
                    MessageBox.Show("No data received.");
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {
                    MessageBox.Show("No data found.");
                    return null;
                }

                // Deserialize only Folder list
                var folderList = JsonConvert.DeserializeObject<List<Folder>>(
                    parsed.data.ToString());

                return new Symbolmodel
                {
                    Data = folderList
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
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
                    MessageBox.Show("No data received.");
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {
                    MessageBox.Show("No data found.");
                    return null;
                }

                // ✅ Correct deserialization
                var dataList = JsonConvert.DeserializeObject<List<SubSymbolModel>>(
                    parsed.data.ToString());

                return new SubSymbolRoot
                {
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
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
                    MessageBox.Show("No data received.");
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {
                    MessageBox.Show("No data found.");
                    return null;
                }

                // ✅ Correct deserialization
                var dataList = JsonConvert.DeserializeObject<List<SubSymbolModel>>(
                    parsed.data.ToString());

                return new SubSymbolRoot
                {
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
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
                    MessageBox.Show("No data received.");
                    return null;
                }

                var jsonString = JsonConvert.SerializeObject(jsonResponse);
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {
                    MessageBox.Show("No data found.");
                    return null;
                }

                // ✅ Correct deserialization
                var dataList = JsonConvert.DeserializeObject<List<SubSymbolModel>>(
                    parsed.data.ToString());

                return new SubSymbolRoot
                {
                    Data = dataList
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
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
