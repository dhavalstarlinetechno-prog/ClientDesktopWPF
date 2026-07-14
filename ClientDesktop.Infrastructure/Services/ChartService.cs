using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.Infrastructure.Services
{
    public class ChartService : IChartService
    {
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;

        public ChartService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        public async Task<List<Chartmodel>> GetHistoryAsync(string symbol, long fromTime, long toTime, string resolution)
        {
            var result = new List<Chartmodel>();

            try
            {              
                string HistoryUrl = CommonHelper.ToReplaceUrl(AppConfig.HistoryDataUrl, _sessionService.PrimaryDomain);
              
                string cleanSymbol = symbol
                    .Replace("-M", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("-m", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
               
                string interval = resolution == "60" ? "1H" : resolution;
                
                var payload = new
                {
                    Symbol = cleanSymbol,
                    fromDate = fromTime * 1000,
                    toDate = toTime * 1000,
                    interval = interval
                };
               
                var apiResponse = await _apiService.PostAsync<HistoryApiResponse>(HistoryUrl, payload);
               
                if (apiResponse?.isSuccess == true && apiResponse.data != null)
                {
                    foreach (var item in apiResponse.data)
                    {
                        result.Add(new Chartmodel
                        {
                            time = item.updateTime / 1000,
                            open = item.openLtp,
                            high = item.highLtp,
                            low = item.lowLtp,
                            close = item.closeLtp,
                            volume = item.volume
                        });
                    }
                }
                else
                {
                    Console.WriteLine($"[ChartService] History API Failed or returned no data.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChartService] Error: {ex.Message}");
            }

            return result;
        }
    }
}
