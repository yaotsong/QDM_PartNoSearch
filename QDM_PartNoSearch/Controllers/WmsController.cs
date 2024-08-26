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
using System.Collections.Generic;

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

        //api抓商店筆數用
        public async Task<List<WmsProduct>> GetProductDataAsync(string apiName)
        {
            List<WmsProduct> productData = new List<WmsProduct>(); //儲存商品資料用
            int currentPage = 1;
            int maxPage;

            //讀取api每一分頁資料後存到list裏頭
            do
            {
                var pageData = await GetPageDataAsync(apiName, currentPage);
                if (pageData == null)
                {
                    _logger.LogError("Failed to retrieve data for page {PageNumber}", currentPage);
                    break;
                }
                if(pageData.Products.Any()) {
                    productData.AddRange(pageData.Products); // Assume `Data` is a list of `YourDataType` in your API response
                }
               

                maxPage = pageData.MaxPage; // Get the max page number from the response
                currentPage++;
            }
            while (currentPage <= maxPage);

            return productData;
        }
        //api抓商品庫存用，並將庫存數字合併商品list
        public async Task<List<WmsProduct>> GetStockDataAsync(List<WmsProduct> data)
        {
            var responsesList = new List<WmsProduct>();

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
                    if (response?.Products != null)
                    {
                        responsesList.AddRange(response.Products);
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

        //api抓訂單資料用，並將訂單商品明細合併到商品list
        public async Task<List<WmsProduct>> GetOrderDataAsync(List<WmsProduct> data)
        {
            // 獲取當前日期
            DateTime today = DateTime.Today;

            // 獲取當前月份的第一天
            DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // 儲存商品資料的 Dictionary，以便快速查找
            var dataDict = data.ToDictionary(item => item.Id, item => item);

            // 當月訂單明細資料
            var orderAllData = new Dictionary<string, int>();

            // 循環從當月第一天到今天
            do
            {
                int currentPage = 1;
                int maxPage;
                var orderList = new List<WmsOrder>();

                do // 讀取 API 每一分頁資料後存到 list 裡頭
                {
                    var pageData = await GetPageDataAsync("條件式篩選訂單", currentPage, "", firstDayOfMonth);
                    if (pageData == null)
                    {
                        _logger.LogError("Failed to retrieve data for page {PageNumber}", currentPage);
                        break;
                    }
                    if (pageData.Orders.Any())
                    {
                        orderList.AddRange(pageData.Orders);
                    }

                    maxPage = pageData.MaxPage; // Get the max page number from the response
                    currentPage++;
                }
                while (currentPage <= maxPage);

                // 彙整訂單資料
                foreach (var order in orderList)
                {
                    if (orderAllData.ContainsKey(order.sku))
                    {
                        orderAllData[order.sku] += order.qty;
                    }
                    else
                    {
                        orderAllData[order.sku] = order.qty;
                    }
                }

                firstDayOfMonth = firstDayOfMonth.AddDays(1);
            }
            while (firstDayOfMonth <= today);

            // 更新商品 data 資料
            foreach (var kvp in orderAllData)
            {
                var sku = kvp.Key;
                var qty = kvp.Value;

                if (dataDict.TryGetValue(sku, out var existingItem))
                {
                    // 如果已存在，則累加 Qty
                    existingItem.Qty += qty;
                }
                else
                {
                    // 如果不存在，則新增條目
                    dataDict[sku] = new WmsProduct { Id = sku, Qty = qty };
                }
            }

            // 將 Dictionary 轉回 List
            return dataDict.Values.ToList();
        }


        private async Task<PageDataResponse> GetPageDataAsync(string apiName, int pageNumber, string skuString = "", DateTime? date=null)
        {
            // 如果 date 是 null，则设置为今天的日期
            DateTime effectiveDate = date ?? DateTime.Today;
            var dateString = effectiveDate.ToString("yyyy/MM/dd");
            var url = "";
            var productList = new List<WmsProduct>();
            var orderList = new List<WmsOrder>();
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
                        case "條件式篩選訂單":
                            url = $"https://reyi-distribution.wms.changliu.com.tw/api_v1/order/order_query.php?nowpage={pageNumber}&pagesize=50&order_date={dateString}";
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
                                                var wmsApi = new WmsProduct
                                                {
                                                    Id = id,
                                                    Name = name,
                                                    Stock = 0,  // 假設默認為 0，根據實際情況設置
                                                    Qty = 0     // 假設默認為 0，根據實際情況設置
                                                };

                                                // 將 WmsApi 物件添加到結果列表中
                                                productList.Add(wmsApi);
                                            }
                                            pageDataResponse.Products = productList;
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
                                                var wmsApi = new WmsProduct
                                                {
                                                    Id = sku,
                                                    Name = name,
                                                    Stock = stock,  // 假設默認為 0，根據實際情況設置
                                                    Qty = 0     // 假設默認為 0，根據實際情況設置
                                                };

                                                // 將 WmsApi 物件添加到結果列表中
                                                productList.Add(wmsApi);
                                                       
                                            }
                                            pageDataResponse.Products = productList;
                                        }
                                        if (apiName == "條件式篩選訂單")
                                        {
                                            //判斷訂單狀態
                                            string status_code;string status_name;string sku;int qty;
                                            if (row.TryGetProperty("status_code", out JsonElement statusElement) && row.TryGetProperty("status_name", out JsonElement statusNameElement))
                                            {
                                                status_code = statusElement.GetString();
                                                status_name = statusNameElement.ToString();
                                                if (status_code == "P" || status_code == "F")
                                                {
                                                    //撈取訂單明細
                                                    if (row.TryGetProperty("products", out JsonElement pdElement))
                                                    {
                                                        foreach (var pd in pdElement.EnumerateArray())
                                                        {
                                                            if (pd.TryGetProperty("items", out JsonElement pdItemsElement))
                                                            {
                                                                foreach (var pdItem in pdItemsElement.EnumerateArray())
                                                                {
                                                                    //判斷查詢庫存時有沒有撈到sku跟訂單數
                                                                    if (pdItem.TryGetProperty("sku", out JsonElement skuElement) &&
                                                                        pdItem.TryGetProperty("qty", out JsonElement qtyElement))
                                                                    {
                                                                        sku = skuElement.GetString();
                                                                        qty = qtyElement.GetInt32();
                                                                        var wmsApi = new WmsOrder
                                                                        {
                                                                            status_code = status_code,
                                                                            status_name = status_name,
                                                                            sku = sku,  // 假設默認為 0，根據實際情況設置
                                                                            qty = qty     // 假設默認為 0，根據實際情況設置
                                                                        };

                                                                        // 將 WmsApi 物件添加到結果列表中
                                                                        orderList.Add(wmsApi);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        
                                                    }
                                                }
                                            }
                                            pageDataResponse.Orders = orderList;
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
            public List<WmsProduct> Products { get; set; }
            public List<WmsOrder> Orders { get; set; }
            public int MaxPage { get; set; }
        }
        //將撈到的商品list，依照50筆拆分出字串
        public static List<string> GroupIds(List<WmsProduct> allData, int groupSize = 50)
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
            var pdData = await GetProductDataAsync("條件式篩選商品");
            pdData = await GetStockDataAsync(pdData);
            var test = await GetOrderDataAsync(pdData);
            return View(pdData);
        }

    }
}

