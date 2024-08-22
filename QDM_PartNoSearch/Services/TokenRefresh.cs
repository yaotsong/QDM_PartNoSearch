﻿using Microsoft.Extensions.Hosting;
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
        private Timer _timer;
        private string _accessToken;
        private readonly string _apiId;
        private readonly string _apiKey;

        public TokenRefreshService(IHttpClientFactory httpClientFactory, ILogger<TokenRefreshService> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiId = configuration["ApiSettings:ApiId"];
            _apiKey = configuration["ApiSettings:ApiKey"];
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
                string apiUrl = "https://reyi-distribution.wms.changliu.com.tw/api_v1/token/authorize.php";
                string apiId = _apiId;  // Replace with actual API ID
                string apiKey = _apiKey;  // Replace with actual API Key

                // Create authorization header
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
                            _accessToken = accessTokenElement.GetString();
                            _logger.LogInformation($"Access token refreshed: {_accessToken}");
                        }
                        else
                        {
                            _logger.LogError("Unable to find access_token in response.");
                        }
                    }
                }
                else
                {
                    _logger.LogError($"API call failed: {response.ReasonPhrase}");
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

