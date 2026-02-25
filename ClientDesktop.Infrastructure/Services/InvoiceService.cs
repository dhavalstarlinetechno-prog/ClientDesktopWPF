using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
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
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<Invoicemodel>> _symbolMode;

        public InvoiceService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }
        public async Task<(bool Success, string ErrorMessage, LedgerAuthData ResponseData)> VerifyUserPasswordAsync(string clientId, string password, string licenseId)
        {
            try
            {
                // ✅ Payload
                var payload = new
                {
                    type = "USER",
                    password,
                    clientId
                };

                // ✅ Resolve server URL
                var serverDomain = _sessionService.ServerListData
                    .FirstOrDefault(x => x.licenseId.ToString() == licenseId)
                    ?.primaryDomain;

                var baseUrl = CommonHelper.ToReplaceUrl(AppConfig.LedgerAuthnticationURL, _sessionService.PrimaryDomain);
                var fullUrl = baseUrl.Contains("http")
                    ? baseUrl
                    : $"{serverDomain}{baseUrl}";

                // ✅ Call ApiService
                var result = await _apiService
                    .PutAsync<LedgerAuthResponse>(fullUrl, payload);

                if (result == null)
                    return (false, CommonMessages.InvalidResponse, null);

                if (!result.isSuccess || result.data == null)
                    return (false,
                        result.successMessage ?? CommonMessages.FailedVerifyUser,
                        null);

                // ✅ Interpret response message
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
                return (false, ex.Message, null);
            }
        }

        //public async Task<Invoicemodel?> InvoiceLoadData(string fromdate, string todate)
        //{
        //    try
        //    {
        //        string url = CommonHelper.ToReplaceUrl(AppConfig.Getinvoice + fromdate + "/" + todate, _sessionService.PrimaryDomain);

        //        var response = await _apiService.GetAsync<Invoicemodel>(url);

        //        if (response == null)
        //        {
        //            MessageBox.Show("No data received.");
        //            return null;
        //        }
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error fetching symbol: " + ex.Message);
        //        return null;
        //    }
        //}

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
                    MessageBox.Show("No data received.");
                    return null;
                }

                // Convert object to string
                var jsonString = JsonConvert.SerializeObject(jsonResponse);

                // Parse JObject
                var parsed = JsonConvert.DeserializeObject<dynamic>(jsonString);

                if (parsed?.data == null)
                {
                    MessageBox.Show("No invoice data found.");
                    return null;
                }

                // Deserialize only data array
                var invoiceList = JsonConvert.DeserializeObject<List<Invoicemodel>>(
                    parsed.data.ToString());

                return invoiceList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching invoice: " + ex.Message);
                return null;
            }
        }
    }
}
