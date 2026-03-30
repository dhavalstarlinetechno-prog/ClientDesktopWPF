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
        private readonly IApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly IRepository<List<FeedbackModel>> _symbolModel;
        
        public FeedbackService(IApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

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

                    // Extension ke hisaab se content type assign karein
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
                // Note: Ensure AppConfig.FeedbackListURL is defined in your config
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
                // TODO: AppConfig.FeedbackURL ki jagah Get details ka actual API endpoint URL pass karein.
                string baseUrl = CommonHelper.ToReplaceUrl(AppConfig.FeedbackRetrieveURL, _sessionService.PrimaryDomain);
                string url = $"{baseUrl}{feedbackId}"; // API endpoint me id pass kar rahe hai

                // Bina naya wrapper model banaye, JObject me response ko map kar rahe hai
                var response = await _apiService.GetAsync<JObject>(url);

                // JObject se check kar rahe hai ki 'isSuccess' true hai ya nahi
                if (response != null && response["isSuccess"]?.Value<bool>() == true)
                {
                    // Existing FeedbackData model me bind kar rahe hai
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
                    // Fix 1: StreamContent ki jagah ByteArrayContent — safer & no stream leak
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);

                    // Fix 2: Content-Type header add kiya — server ko file type pata chalega
                    string ext = Path.GetExtension(filePath).ToLower();
                    string contentType = (ext == ".png") ? "image/png" :
                                         (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" :
                                         "application/octet-stream";

                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    formData.Add(fileContent, "files", Path.GetFileName(filePath));
                }

                // Fix 3: PostAsync ki jagah PostRawAsync — multipart ke liye yahi sahi hai
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
                // URL prepare karo (AppConfig mathi URL engine replace kari ne)
                string baseUrl = CommonHelper.ToReplaceUrl(AppConfig.FeedbackDeleteURL, _sessionService.PrimaryDomain);
                string url = $"{baseUrl}{feedbackId}";

                // ApiService ni DeleteAsync call karo
                // Jo body ma kai na mokalvu hoy to empty object '{}' mokli sakay
                var response = await _apiService.DeleteAsync<FeedbackResponse>(url, new { id = feedbackId });

                return response;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(DeleteFeedbackAsync), ex);
                return new FeedbackResponse { IsSuccess = false, Exception = ex.Message };
            }
        }
    }
}
