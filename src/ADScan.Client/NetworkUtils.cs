using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using System.Threading;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Net;

namespace ADScan.Client
{
    public class NetworkUtils
    {
        private static string _baseApiUrl = "http://95.111.235.29/";
        private static readonly Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => GetClient(), LazyThreadSafetyMode.ExecutionAndPublication);
        private static JsonSerializerSettings _serializerSettings;

        private static HttpClient GetClient()
        {
            var httpClient = new HttpClient() { BaseAddress = new Uri(_baseApiUrl) };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        public async static Task<TResult> PostAsync<TResult>(string uri, object data, string header = "")
        {
            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore
            };
            _serializerSettings.Converters.Add(new StringEnumConverter());

            var content = new StringContent(JsonConvert.SerializeObject(data));

            HttpClient httpClient = await GetOrCreateHttpClient();

            if (!string.IsNullOrEmpty(header))
            {
                AddHeaderParameter(httpClient, header);
            }

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await httpClient.PostAsync(uri, content).ConfigureAwait(false);

            await HandleResponse(response).ConfigureAwait(false);
            string serialized = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            TResult result = JsonConvert.DeserializeObject<TResult>(serialized, _serializerSettings);

            return result;
        }

        private static async Task<HttpClient> GetOrCreateHttpClient()
        {
            var authToken = await SecureStorage.GetAsync("authToken");
            var httpClient = _httpClient.Value;

            httpClient.DefaultRequestHeaders.Authorization =
                !string.IsNullOrEmpty(authToken)
                    ? new AuthenticationHeaderValue("Bearer", authToken)
                    : null;

            return httpClient;
        }

        private static void AddHeaderParameter(HttpClient httpClient, string parameter)
        {
            if (httpClient == null)
                return;

            if (string.IsNullOrEmpty(parameter))
                return;

            httpClient.DefaultRequestHeaders.Add(parameter, Guid.NewGuid().ToString());
        }

        private static async Task HandleResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Forbidden ||
                        response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception("Authentication Error");// ServiceAuthenticationException(content);
                }
                throw new Exception("Service Error"); // HttpRequestServiceException(response.StatusCode, content);
            }
        }
    }
}
