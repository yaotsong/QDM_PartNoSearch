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
            _flavorApiKey = configuration["ApiSettings:FlavorApiKey"];
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TokenRefreshService 正在啟動。");
            _timer = new Timer(RefreshToken, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }

        private async void RefreshToken(object state)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("NoCertValidationClient");

                // 刷新暢流 API 的存取令牌
                await RefreshTokenForApi(
                    httpClient,
                    "https://192.168.1.100/api_v1/token/authorize.php",
                    _flavorApiId,
                    _flavorApiKey,
                    "FlavorAccessToken",
                    "暢流"
                );

                // 刷新日翊 API 的存取令牌
                await RefreshTokenForApi(
                    httpClient,
                    "https://reyi-distribution.wms.changliu.com.tw/api_v1/token/authorize.php",
                    _reyiApiId,
                    _reyiApiKey,
                    "ReyiAccessToken",
                    "日翊"
                );

            }
            catch (Exception ex)
            {
                _logger.LogError($"刷新存取令牌時發生錯誤：{ex.Message}");
            }
        }

        private async Task RefreshTokenForApi(HttpClient httpClient, string apiUrl, string apiId, string apiKey, string cacheKey, string apiName)
        {
            try
            {
                // 建立授權標頭
                string authString = $"{apiId}:{apiKey}";
                string base64AuthString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authString));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64AuthString);

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                        {
                            string accessToken = accessTokenElement.GetString();
                            _cache.Set(cacheKey, accessToken, TimeSpan.FromHours(1)); // 儲存到快取，設定 1 小時過期
                            _logger.LogInformation($"{apiName} 存取令牌已刷新並快取：{accessToken}");
                        }
                        else
                        {
                            _logger.LogError($"無法在 {apiName} 的回應中找到 access_token。");
                        }
                    }
                }
                else
                {
                    _logger.LogError($"{apiName} API 呼叫失敗：{response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{apiName} 存取令牌刷新時發生錯誤：{ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TokenRefreshService 正在停止。");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
