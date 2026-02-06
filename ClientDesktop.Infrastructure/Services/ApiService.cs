using ClientDesktop.Core.Interfaces;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ClientDesktop.Infrastructure.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _http;

        public ApiService()
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        private void AddAuthHeader()
        {
            _http.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
            }
        }

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
            catch { return default; }
        }

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
            catch { return default; }
        }

        public async Task<HttpResponseMessage> PostRawAsync(string url, HttpContent content)
        {
            try
            {
                AddAuthHeader();
                return await _http.PostAsync(url, content);
            }
            catch
            {
                // Return a basic error response if connection fails completely
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = "Service Unavailable / Connection Failed"
                };
            }
        }

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
            catch { return default; }
        }
    }
}