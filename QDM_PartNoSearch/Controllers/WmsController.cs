using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Azure.Core;
using System.Net.Http;
using QDM_PartNoSearch.Models;

namespace QDM_PartNoSearch.Controllers
{
    public class WmsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WmsController> _logger;

        public WmsController(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<WmsController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _cache = cache;
            _logger = logger;
        }
        public async Task<List<string>> GetAllDataAsync()
        {
            var allData = new List<string>();
            int currentPage = 1;
            int maxPage;
            string apiName = "條件式篩選商品";
            do
            {
                var pageData = await GetPageDataAsync(apiName, currentPage);
                if (pageData == null)
                {
                    _logger.LogError("Failed to retrieve data for page {PageNumber}", currentPage);
                    break;
                }

                allData.AddRange(pageData.Data); // Assume `Data` is a list of `YourDataType` in your API response

                maxPage = pageData.MaxPage; // Get the max page number from the response
                currentPage++;
            }
            while (currentPage <= maxPage);

            return allData;
        }
        // 全域變數來存儲提取的資料
         public List<string> ResultList { get; private set; } = new List<string>();
        private async Task<PageDataResponse> GetPageDataAsync(string apiurl, int pageNumber)
        {
            var url = "";
            var resultList = new List<string>();
            if (_cache.TryGetValue("AccessToken", out string _accessToken))
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    switch (apiurl)
                    {
                        case "條件式篩選商品":
                            url = $"https://reyi-distribution.wms.changliu.com.tw/api_v1/product/pro_query.php?nowpage={pageNumber}&pagesize=100";
                            break;
                        default:
                            url = "";
                            break;
                    }
                    HttpResponseMessage response = await _httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(content))
                        {
                            var pageDataResponse = new PageDataResponse();

                            if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement))
                            {
                                if (dataElement.TryGetProperty("rows", out JsonElement rowsElement) && rowsElement.ValueKind == JsonValueKind.Array)
                                {
                                    // 遍歷 rows 陣列
                                    foreach (var row in rowsElement.EnumerateArray())
                                    {
                                        // 提取 id 和 name
                                        if (row.TryGetProperty("id", out JsonElement idElement) &&
                                            row.TryGetProperty("name", out JsonElement nameElement))
                                        {
                                            string id = idElement.GetString();
                                            string name = nameElement.GetString();

                                            // 將 id 和 name 存儲到 List 中
                                            resultList.Add($"ID: {id}, Name: {name}");
                                            pageDataResponse.Data = resultList;
                                        }
                                    }
                                }

                                if (dataElement.TryGetProperty("maxpage", out JsonElement maxPageElement))
                                {
                                    pageDataResponse.MaxPage = maxPageElement.GetInt32();
                                }
                            }
                            
                            return pageDataResponse;
                        }
                    }
                    else
                    {
                        _logger.LogError("API call failed: {ReasonPhrase}", response.ReasonPhrase);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error occurred while making API call: {Message}", ex.Message);
                    return null;
                }
            }
            else
            {
                _logger.LogWarning("Access token not found in cache.");
                return null;
            }
        }

        public class PageDataResponse
        {
            public List<string> Data { get; set; }
            public int MaxPage { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetApiInfo(string url)
        {
            if (_cache.TryGetValue("AccessToken", out string? accessToken))
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    HttpResponseMessage response = await _httpClient.GetAsync(url);

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
                                // 先獲取 maxpage 的值，再去call對應的apiAction
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
            else
            {
                _logger.LogWarning("Access token not found in cache.");
                return StatusCode(500, "Access token not found in cache.");
            }
        }

        public IActionResult StoreNum()
        {
            Task<List<string>> task = GetAllDataAsync();
            return View();
        }

    }
}

