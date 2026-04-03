using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDesktop.Infrastructure.Services
{
    public class FeedbackService
    {
        #region Variables

        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<FeedbackModel>> _symbolModel;

        #endregion Variables

        #region Constructor
        public FeedbackService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        #endregion Constructor

        #region Methods
        public async Task<FeedbackResponse> GenerateFeedbackAsync(string feedbackSubject, string feedbackMessage, string filePath)
        {
            try
            {             
                string url = CommonHelper.ToReplaceUrl(AppConfig.FeedbackGenerateURL, _sessionService.PrimaryDomain);
               
                var formData = new MultipartFormDataContent();
                
                formData.Add(new StringContent(feedbackSubject ?? string.Empty), "feedbackSubject");
                formData.Add(new StringContent(feedbackMessage ?? string.Empty), "feedbackMessage");
               
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    
                    string ext = Path.GetExtension(filePath).ToLower();
                    string contentType = (ext == ".png") ? "image/png" :
                                         (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" :
                                         "application/octet-stream";

                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    formData.Add(fileContent, "files", Path.GetFileName(filePath));
                }
              
                HttpResponseMessage response = await _apiService.PostRawAsync(url, formData);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<FeedbackResponse>(responseContent);
                }

                return new FeedbackResponse
                {
                    IsSuccess = false,
                    Exception = $"Server returned HTTP Code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {                
                Console.WriteLine($"Error occurred: {ex.Message}");
                return new FeedbackResponse
                {
                    IsSuccess = false,
                    Exception = ex.Message
                };
            }
        }
        public async Task<FeedbackListResponse> GetFeedbackListAsync()
        {
            try
            {               
                string url = CommonHelper.ToReplaceUrl(AppConfig.FeedbackURL, _sessionService.PrimaryDomain);

                var response = await _apiService.GetAsync<FeedbackListResponse>(url);
                return response;
            }
            catch (Exception ex)
            {
                return new FeedbackListResponse
                {
                    IsSuccess = false,
                    Exception = ex.Message
                };
            }
        }
        public async Task<FeedbackData> GetFeedbackDetailsAsync(int feedbackId)
        {
            try
            {                
                string baseUrl = CommonHelper.ToReplaceUrl(AppConfig.FeedbackRetrieveURL, _sessionService.PrimaryDomain);
                string url = $"{baseUrl}{feedbackId}";
              
                var response = await _apiService.GetAsync<JObject>(url);
               
                if (response != null && response["isSuccess"]?.Value<bool>() == true)
                {                   
                    return response["data"]?.ToObject<FeedbackData>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while fetching feedback details: {ex.Message}");
                return null;
            }
        }
        public async Task<FeedbackReplyResponse> ReplyFeedbackAsync(int feedbackid, string feedbackMessage, string filePath)
        {
            try
            {
                string url = CommonHelper.ToReplaceUrl(AppConfig.FeedbackReplayURL, _sessionService.PrimaryDomain);

                var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(feedbackid.ToString()), "feedbackId");
                formData.Add(new StringContent(feedbackMessage ?? string.Empty), "feedbackMessage");

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {                  
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                  
                    string ext = Path.GetExtension(filePath).ToLower();
                    string contentType = (ext == ".png") ? "image/png" :
                                         (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" :
                                         "application/octet-stream";

                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    formData.Add(fileContent, "files", Path.GetFileName(filePath));
                }
               
                HttpResponseMessage response = await _apiService.PostRawAsync(url, formData);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<FeedbackReplyResponse>(responseContent);
                }

                return new FeedbackReplyResponse
                {
                    IsSuccess = false,
                    SuccessMessage = $"Server returned HTTP Code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                return new FeedbackReplyResponse
                {
                    IsSuccess = false,
                    SuccessMessage = ex.Message
                };
            }
        }
        public async Task<FeedbackResponse> DeleteFeedbackAsync(int feedbackId)
        {
            try
            {               
                string baseUrl = CommonHelper.ToReplaceUrl(AppConfig.FeedbackDeleteURL, _sessionService.PrimaryDomain);
                string url = $"{baseUrl}{feedbackId}";             
                var response = await _apiService.DeleteAsync<FeedbackResponse>(url, new { id = feedbackId });

                return response;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(DeleteFeedbackAsync), ex);
                return new FeedbackResponse { IsSuccess = false, Exception = ex.Message };
            }
        }

        #endregion Methods
    }
}
