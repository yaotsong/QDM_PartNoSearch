using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace QDM_PartNoSearch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WmsApiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiId;
        private readonly string _apiKey;

        public WmsApiController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiId = configuration["ApiSettings:ApiId"];
            _apiKey = configuration["ApiSettings:ApiKey"];
        }

        //取得暢流accesstoken
        [HttpGet]
        public async Task<IActionResult> CallExternalApi()
        {
            try
            {
                string apiUrl = "https://reyi-distribution.wms.changliu.com.tw/api_v1/token/authorize.php";

                // 創建認證header
                string authString = $"{_apiId}:{_apiKey}";
                string base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64AuthString);

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    // 使用System.Text.Json解析JSON
                    using JsonDocument doc = JsonDocument.Parse(content);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("data", out JsonElement dataElement) &&
                        dataElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                    {
                        string accessToken = accessTokenElement.GetString();
                        return Ok(accessToken);
                    }
                    else
                    {
                        return BadRequest("無法在響應中找到access_token");
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode, "API調用失敗");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"發生錯誤: {ex.Message}");
            }
        }
    }
}
