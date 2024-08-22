using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace QDM_PartNoSearch.Controllers
{
    public class WmsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiId;
        private readonly string _apiKey;
        private readonly string _accessToken;

        public WmsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiId = configuration["ApiSettings:ApiId"];
            _apiKey = configuration["ApiSettings:ApiKey"];

        }

        //取得暢流accesstoken
        [HttpGet]
        public async Task<string> CallAccessTokenApi()
        {
            try
            {
                string apiUrl = "https://reyi-distribution.wms.changliu.com.tw/api_v1/token/authorize.php";

                // 創建認證 header
                string authString = $"{_apiId}:{_apiKey}";
                string base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64AuthString);

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                        {
                            return accessTokenElement.GetString();
                        }
                        else
                        {
                            return "無法在響應中找到 access_token";
                        }
                    }
                }
                else
                {
                    // 返回失敗信息
                    return $"API 呼叫失敗: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                // 返回錯誤信息
                return $"發生錯誤: {ex.Message}";
            }
        }
        [HttpGet]
        public async Task<IActionResult> CountPageNum(string url)
        {
            try
            {
                string apiUrl = url;

                // 獲取token
                string accessToken = await CallAccessTokenApi();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        JsonElement root = doc.RootElement;
                        // 檢查是否包含 "data" 屬性
                        if (root.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("maxpage", out JsonElement maxPageElement))
                        {
                            // 獲取 maxpage 的值
                            int maxPage = maxPageElement.GetInt32();

                            return Ok($"Max Page: {maxPage}");
                        }
                        else
                        {
                            // 如果 "data" 或 "maxpage" 屬性不存在，返回相應錯誤信息
                            return BadRequest("無法在響應中找到 'data' 或 'maxpage'");
                        }

                    }
                }
                else
                {
                    // 返回失敗信息
                    return NotFound($"API 呼叫失敗: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                // 返回錯誤信息
                return NotFound($"發生錯誤: {ex.Message}");
            }
        }

        public async Task<IActionResult> StoreNum()
        {
            // 呼叫 CallExternalApi 方法並獲取 accessToken
            string accessToken = await CallAccessTokenApi();

            // 將 accessToken 儲存在 ViewBag 中，以便在視圖中使用
            ViewBag.AccessToken = accessToken;

            return View();
        }

    }
}

