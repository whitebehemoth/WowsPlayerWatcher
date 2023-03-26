using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;

namespace WowsPlayerWatcher.Services
{
    public class WgHttpClient<T> : HttpClient
    {
        public WgHttpClient()
        {
            DefaultRequestHeaders.Add(HeaderNames.Connection, "keep-alive");
            DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
        }
        public async Task<WgResponce<T>?> GetData(Uri uri)
        {
            var responce = await GetAsync(uri);
            WgResponce<T>? responceJson = await responce.Content.ReadFromJsonAsync<WgResponce<T>>();
            return responceJson;
        }
    }
}
