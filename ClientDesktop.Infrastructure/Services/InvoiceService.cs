using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClientDesktop.Infrastructure.Services
{
    public class InvoiceService
    {
        #region Variables

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        //private readonly IRepository<List<Invoicemodel>>? _symbolMode;

        #endregion Variables

        #region Constructor
        public InvoiceService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        #endregion Constructor

        #region Methods
        public async Task<(bool Success, string? ErrorMessage, LedgerAuthData? ResponseData)> VerifyUserPasswordAsync(string clientId, string password, string licenseId)
        {
            try
            {                
                var payload = new
                {
                    type = "USER",
                    password,
                    clientId
                };
              
                var serverDomain = _sessionService.ServerListData
                    .FirstOrDefault(x => x.licenseId.ToString() == licenseId)
                    ?.primaryDomain;

                var baseUrl = CommonHelper.ToReplaceUrl(AppConfig.LedgerAuthnticationURL, _sessionService.PrimaryDomain);
                var fullUrl = baseUrl.Contains("http")
                    ? baseUrl
                    : $"{serverDomain}{baseUrl}";
                
                var result = await _apiService
                    .PutAsync<LedgerAuthResponse>(fullUrl, payload);

                if (result == null)
                    return (false, CommonMessages.InvalidResponse, null);

                if (!result.isSuccess || result.data == null)
                    return (false,
                        result.successMessage ?? CommonMessages.FailedVerifyUser,
                        null);
             
                var message = result.data.msg?.FirstOrDefault()?.Trim() ?? string.Empty;

                if (message.Equals(CommonMessages.WrongPassword, StringComparison.OrdinalIgnoreCase))
                    return (false, CommonMessages.WrongPassword, result.data);

                if (message.Contains("maximum", StringComparison.OrdinalIgnoreCase))
                    return (false, CommonMessages.MaximumPasswordAttempts, result.data);

                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return (false, CommonMessages.UserNotFound, result.data);

                if (message.Equals("OK !", StringComparison.OrdinalIgnoreCase))
                    return (true, null, result.data);

                return (false, message, result.data);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(VerifyUserPasswordAsync), ex.Message);
                return (false, ex.Message, null);
            }
        }
        public async Task<List<Invoicemodel>?> InvoiceLoadData(string fromdate, string todate)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(
                    AppConfig.Getinvoice + fromdate + "/" + todate,
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
                
                var invoiceList = JsonConvert.DeserializeObject<List<Invoicemodel>>(
                    parsed.data.ToString());

                return invoiceList;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(InvoiceLoadData) + ex.Message);
                return null;
            }
        }

        #endregion Methods
    }
}
