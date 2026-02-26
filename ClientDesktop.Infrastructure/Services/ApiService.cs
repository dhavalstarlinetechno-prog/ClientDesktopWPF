using ClientDesktop.Core.Interfaces;
using Newtonsoft.Json;
using System.Net.Http.Headers;
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
            try
            {
                AddAuthHeader();
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode) return default;

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Sends a POST request with JSON serialized data to the specified URL.
        /// </summary>
        public async Task<T> PostAsync<T>(string url, object data)
        {
            try
            {
                AddAuthHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(url, content);

                if (!response.IsSuccessStatusCode) return default;

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Sends a POST request with URL-encoded form data to the specified URL.
        /// </summary>
        public async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> data)
        {
            try
            {
                AddAuthHeader();
                var content = new FormUrlEncodedContent(data);
                var response = await _http.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Sends a raw POST request with custom HttpContent to the specified URL.
        /// </summary>
        public async Task<HttpResponseMessage> PostRawAsync(string url, HttpContent content)
        {
            try
            {
                AddAuthHeader();
                return await _http.PostAsync(url, content);
            }
            catch
            {
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
            try
            {
                AddAuthHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PutAsync(url, content);

                if (!response.IsSuccessStatusCode) return default;

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            catch
            {
                return default;
            }
        }

        #endregion

        #region Private Methods

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