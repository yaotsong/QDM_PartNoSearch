using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace QDM_PartNoSearch.Services
{
    public class TokenRefreshService : IHostedService, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenRefreshService> _logger;
        private readonly IMemoryCache _cache;
        private Timer _timer;
        private readonly string _reyiApiId;
        private readonly string _reyiApiKey;
        private readonly string _flavorApiId;
        private readonly string _flavorApiKey;

        public TokenRefreshService(IHttpClientFactory httpClientFactory, ILogger<TokenRefreshService> logger, IMemoryCache cache, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cache = cache;
            _reyiApiId = configuration["ApiSettings:ReyiApiId"];
            _reyiApiKey = configuration["ApiSettings:ReyiApiKey"];
            _flavorApiId = configuration["ApiSettings:FlavorApiId"];
            _flavorApiKey = configuration["ApiSettings:FlavorApuKey"];
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TokenRefreshService is starting.");
            _timer = new Timer(RefreshToken, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }

        private async void RefreshToken(object state)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                //日翊暢流API
                string reyiApiUrl = "https://reyi-distribution.wms.changliu.com.tw/api_v1/token/authorize.php";
                string reyiApiId = _reyiApiId;
                string reyiApiKey = _reyiApiKey;
                

                // Create authorization header
                //日翊暢流
                string reyiAuthString = $"{reyiApiId}:{reyiApiKey}";
                string reyiBase64AuthString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(reyiAuthString));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", reyiBase64AuthString);
               

                HttpResponseMessage response = await httpClient.GetAsync(reyiApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                        {
                            string accessToken = accessTokenElement.GetString();
                            _cache.Set("ReyiAccessToken", accessToken, TimeSpan.FromHours(1)); // Store in cache with 1 hour expiration
                            _logger.LogInformation($"Reyi Access token refreshed and cached: {accessToken}");
                        }
                        else
                        {
                            _logger.LogError("Unable to findReyi  access_token in response.");
                        }
                    }
                }
                else
                {
                    _logger.LogError($"Reyi API call failed: {response.ReasonPhrase}");
                }


                //暢流API
                string flavorApiUrl = "https://192.168.1.100/api_v1/token/authorize.php";
                string flavorApiId = _reyiApiId;
                string flavorApiKey = _reyiApiKey;
                //暢流
                string flavorAuthString = $"{flavorApiId}:{flavorApiKey}";
                string flavorBase64AuthString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(flavorAuthString));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", flavorBase64AuthString);

                HttpResponseMessage flavor_response = await httpClient.GetAsync(flavorApiUrl);

                if (flavor_response.IsSuccessStatusCode)
                {
                    string content = await flavor_response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                        {
                            string accessToken = accessTokenElement.GetString();
                            _cache.Set("FlavorAccessToken", accessToken, TimeSpan.FromHours(1)); // Store in cache with 1 hour expiration
                            _logger.LogInformation($"Flavor Access token refreshed and cached: {accessToken}");
                        }
                        else
                        {
                            _logger.LogError("Unable to find Flavor access_token in response.");
                        }
                    }
                }
                else
                {
                    _logger.LogError($"Flavor API call failed: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while refreshing token: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TokenRefreshService is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
