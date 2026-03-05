using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Logger;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for handling HTTP API requests and applying authentication headers.
    /// </summary>
    public class ApiService : IApiService
    {
        #region Fields

        private readonly HttpClient _http;
        private readonly SessionService _sessionService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ApiService class.
        /// </summary>
        public ApiService(SessionService sessionService)
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(30);
            _sessionService = sessionService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a GET request to the specified URL and returns the deserialized JSON response.
        /// </summary>
        public async Task<T> GetAsync<T>(string url)
        {
            if (!ValidateRequest(url)) return default;

            try
            {
                AddAuthHeader();
                var response = await _http.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorizedAccess(url);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    FileLogger.ApplicationLog(nameof(GetAsync), $"URL: {url} failed with Status: {response.StatusCode}");
                    return default;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetAsync), ex);
                return default;
            }
        }

        /// <summary>
        /// Sends a POST request with JSON serialized data to the specified URL.
        /// </summary>
        public async Task<T> PostAsync<T>(string url, object data)
        {
            if (!ValidateRequest(url)) return default;

            try
            {
                AddAuthHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(url, content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorizedAccess(url);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    FileLogger.ApplicationLog(nameof(PostAsync), $"URL: {url} failed with Status: {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PostAsync), ex);
                return default;
            }
        }

        /// <summary>
        /// Sends a POST request with URL-encoded form data to the specified URL.
        /// </summary>
        public async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> data)
        {
            if (!ValidateRequest(url)) return default;

            try
            {
                AddAuthHeader();
                var content = new FormUrlEncodedContent(data);
                var response = await _http.PostAsync(url, content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorizedAccess(url);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    FileLogger.ApplicationLog(nameof(PostFormAsync), $"URL: {url} failed with Status: {response.StatusCode}");
                    return default;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PostFormAsync), ex);
                return default;
            }
        }

        /// <summary>
        /// Sends a raw POST request with custom HttpContent to the specified URL.
        /// </summary>
        public async Task<HttpResponseMessage> PostRawAsync(string url, HttpContent content)
        {
            if (!ValidateRequest(url))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = "Request Blocked: No internet, not logged in, or token expired"
                };
            }

            try
            {
                AddAuthHeader();
                var response = await _http.PostAsync(url, content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorizedAccess(url);
                }
                else if (!response.IsSuccessStatusCode)
                {
                    FileLogger.ApplicationLog(nameof(PostRawAsync), $"URL: {url} failed with Status: {response.StatusCode}");
                }

                return response;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PostRawAsync), ex);
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = "Service Unavailable / Connection Failed"
                };
            }
        }

        /// <summary>
        /// Sends a PUT request with JSON serialized data to the specified URL.
        /// </summary>
        public async Task<T> PutAsync<T>(string url, object data)
        {
            if (!ValidateRequest(url)) return default;

            try
            {
                AddAuthHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PutAsync(url, content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorizedAccess(url);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    FileLogger.ApplicationLog(nameof(PutAsync), $"URL: {url} failed with Status: {response.StatusCode}");
                    return default;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PutAsync), ex);
                return default;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks network availability, user authentication, and token expiry before allowing an API call.
        /// </summary>
        private bool ValidateRequest(string url)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                FileLogger.Log("Network", "No internet connection.");
                return false;
            }

            if (!_sessionService.IsLoggedIn)
            {
                string lowerUrl = url.ToLowerInvariant();

                if (lowerUrl.Contains("login") ||
                    lowerUrl.Contains("auth") ||
                    lowerUrl.Contains("token") ||
                    lowerUrl.Contains("server"))
                {
                    return true;
                }

                FileLogger.ApplicationLog(nameof(ValidateRequest), $"API call blocked: User is not logged in. URL: {url}");
                return false;
            }

            if (_sessionService.Expiration.HasValue && DateTime.Now >= _sessionService.Expiration.Value)
            {
                FileLogger.ApplicationLog(nameof(ValidateRequest), $"API call blocked: Token Expired. URL: {url}");
                WeakReferenceMessenger.Default.Send(new UserAuthEvent(false, string.Empty));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Triggers a global logout signal when the server returns a 401 Unauthorized status.
        /// </summary>
        private void HandleUnauthorizedAccess(string url)
        {
            FileLogger.ApplicationLog("API Security", $"401 Unauthorized received for URL: {url}. Triggering auto-logout.");
            WeakReferenceMessenger.Default.Send(new UserAuthEvent(false, string.Empty));
        }

        /// <summary>
        /// Adds the authorization bearer token to the HTTP request headers if the user is currently logged in.
        /// </summary>
        private void AddAuthHeader()
        {
            _http.DefaultRequestHeaders.Authorization = null;
            if (_sessionService.IsLoggedIn)
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sessionService.Token);
            }
        }

        #endregion
    }
}