using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Azure.Core;
using System.Net.Http;
using QDM_PartNoSearch.Models;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Threading.Tasks;

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
        public List<WmsApi> allData = new List<WmsApi>();
        //api抓頁面筆數用，ex:查詢商場商品資料、訂單查詢
        public async Task<List<WmsApi>> GetAllDataAsync(string apiName)
        {
            int currentPage = 1;
            int maxPage;

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
        //api抓庫存用，ex:查詢商品庫存
        public async Task<List<WmsApi>> GetStockDataAsync(List<WmsApi> data)
        {
            var responsesList = new List<WmsApi>();

            if (data.Any())
            {
                var allSkuData = GroupIds(data);
                var tasks = new List<Task<PageDataResponse>>();

                // 建立所有異步任務
                foreach (var item in allSkuData)
                {
                    tasks.Add(GetPageDataAsync("查詢庫存", 1, item));
                }

                // 等待所有異步操作完成
                var responses = await Task.WhenAll(tasks);

                // 合併所有查詢結果
                foreach (var response in responses)
                {
                    if (response?.Data != null)
                    {
                        responsesList.AddRange(response.Data);
                    }
                }
            }

            // 將庫存數據存儲到字典中以便快速查詢
            var stockDictionary = responsesList.ToDictionary(b => b.Id, b => b.Stock);

            // 更新 allData 列表中的庫存數據
            foreach (var allItem in data)
            {
                if (stockDictionary.TryGetValue(allItem.Id, out var stock))
                {
                    allItem.Stock = stock;
                }
            }

            return data;
        }


        private async Task<PageDataResponse> GetPageDataAsync(string apiName, int pageNumber, string skuString = "")
        {
            var url = "";
            var resultList = new List<WmsApi>();
            if (_cache.TryGetValue("AccessToken", out string _accessToken))
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    switch (apiName)
                    {
                        case "條件式篩選商品":
                            url = $"https://reyi-distribution.wms.changliu.com.tw/api_v1/product/pro_query.php?nowpage={pageNumber}&pagesize=100";
                            break;
                        case "查詢庫存":
                            url = $"https://reyi-distribution.wms.changliu.com.tw/api_v1/inventory/stock_query.php?sku={skuString}";
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
                                        if (apiName == "條件式篩選商品")
                                        {
                                            // 提取 id 和 name
                                            if (row.TryGetProperty("id", out JsonElement idElement) &&
                                                row.TryGetProperty("name", out JsonElement nameElement))
                                            {
                                                string id = idElement.GetString();
                                                string name = nameElement.GetString();
                                                //int stock = 
                                                // 創建 WmsApi 物件並設置屬性
                                                var wmsApi = new WmsApi
                                                {
                                                    Id = id,
                                                    Name = name,
                                                    Stock = 0,  // 假設默認為 0，根據實際情況設置
                                                    Qty = 0     // 假設默認為 0，根據實際情況設置
                                                };

                                                // 將 WmsApi 物件添加到結果列表中
                                                resultList.Add(wmsApi);
                                            }
                                        }
                                        if (apiName == "查詢庫存")
                                        {
                                            //判斷查詢庫存時有沒有撈到sku跟庫存數
                                            if (row.TryGetProperty("sku", out JsonElement skuElement) &&
                                                row.TryGetProperty("stock", out JsonElement stockElement) &&
                                                row.TryGetProperty("name", out JsonElement nameElement))
                                            {
                                                string sku = skuElement.GetString();
                                                int stock = stockElement.GetInt32();
                                                string name = nameElement.ToString();
                                                var wmsApi = new WmsApi
                                                {
                                                    Id = sku,
                                                    Name = name,
                                                    Stock = stock,  // 假設默認為 0，根據實際情況設置
                                                    Qty = 0     // 假設默認為 0，根據實際情況設置
                                                };

                                                // 將 WmsApi 物件添加到結果列表中
                                                resultList.Add(wmsApi);
                                                
                                            }
                                        }
                                    }
                                    pageDataResponse.Data = resultList;
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
            public List<WmsApi> Data { get; set; }
            public int MaxPage { get; set; }
        }
        //將撈到的商品list，依照50筆拆分出字串
        public static List<string> GroupIds(List<WmsApi> allData, int groupSize = 50)
        {
            // 提取所有 Id
            List<string> ids = allData.Select(x => x.Id).ToList();

            // 将 Id 按每 groupSize 个分组
            var groupedIds = ids
                .Select((id, index) => new { id, index })
                .GroupBy(x => x.index / groupSize)
                .Select(g => g.Select(x => x.id).ToList())
                .ToList();

            // 将每组 Id 转换为字符串
            List<string> groupedStrings = groupedIds
                .Select(group => string.Join(",", group))
                .ToList();

            return groupedStrings;
        }

        public async Task<IActionResult> StoreNum()
        {
            var allData = await GetAllDataAsync("條件式篩選商品");
            allData = await GetStockDataAsync(allData);
            return View(allData);
        }

    }
}

