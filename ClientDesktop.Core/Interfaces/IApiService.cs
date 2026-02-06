namespace ClientDesktop.Core.Interfaces
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string url);

        Task<T> PostAsync<T>(string url, object data);

        Task<T> PostFormAsync<T>(string url, Dictionary<string, string> data);

        Task<HttpResponseMessage> PostRawAsync(string url, HttpContent content);

        Task<T> PutAsync<T>(string url, object data);
    }
}