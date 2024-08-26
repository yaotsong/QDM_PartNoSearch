using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
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

        // API 抓商店筆數用
        public async Task<List<WmsProduct>> GetProductDataAsync()
        {
            var productData = new List<WmsProduct>();
            int currentPage = 1;
            int maxPage;

            do
            {
                var pageData = await GetPageDataAsync("條件式篩選商品", currentPage);
                if (pageData?.Products != null)
                {
                    productData.AddRange(pageData.Products);
                }

                if (pageData == null || !pageData.Products.Any())
                {
                    _logger.LogError("無法擷取第 {PageNumber} 頁的資料", currentPage);
                    break;
                }

                maxPage = pageData.MaxPage;
                currentPage++;
            }
            while (currentPage <= maxPage);

            return productData;
        }

        // API 抓商品庫存用，並將庫存數字合併商品 list
        public async Task<List<WmsProduct>> GetStockDataAsync(List<WmsProduct> data)
        {
            var responsesList = new List<WmsProduct>();

            if (data.Any())
            {
                var allSkuData = GroupIds(data);
                var tasks = allSkuData.Select(item => GetPageDataAsync("查詢庫存", 1, item)).ToList();
                var responses = await Task.WhenAll(tasks);

                foreach (var response in responses)
                {
                    if (response?.Products != null)
                    {
                        responsesList.AddRange(response.Products);
                    }
                }
            }

            var stockDictionary = responsesList.ToDictionary(b => b.Id, b => b.Stock);

            foreach (var item in data)
            {
                if (stockDictionary.TryGetValue(item.Id, out var stock))
                {
                    item.Stock = stock;
                }
            }

            return data;
        }

        // API 抓訂單資料用，並將訂單商品明細合併到商品 list
        public async Task<List<WmsProduct>> GetOrderDataAsync(List<WmsProduct> data)
        {
            DateTime today = DateTime.Today;
            DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            var dataDict = data.ToDictionary(item => item.Id);
            var orderAllData = new Dictionary<string, int>();

            do
            {
                int currentPage = 1;
                int maxPage;
                var orderList = new List<WmsOrder>();

                do
                {
                    var pageData = await GetPageDataAsync("條件式篩選訂單", currentPage, "", firstDayOfMonth);
                    if (pageData?.Orders != null)
                    {
                        orderList.AddRange(pageData.Orders);
                    }

                    if (pageData == null)
                    {
                        _logger.LogError("無法擷取第 {PageNumber} 頁的資料", currentPage);
                        break;
                    }

                    maxPage = pageData.MaxPage;
                    currentPage++;
                }
                while (currentPage <= maxPage);

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

            foreach (var kvp in orderAllData)
            {
                if (dataDict.TryGetValue(kvp.Key, out var existingItem))
                {
                    existingItem.Qty += kvp.Value;
                }
                else
                {
                    dataDict[kvp.Key] = new WmsProduct { Id = kvp.Key, Qty = kvp.Value };
                }
            }

            return dataDict.Values.ToList();
        }

        private async Task<PageDataResponse> GetPageDataAsync(string apiName, int pageNumber, string skuString = "", DateTime? date = null)
        {
            DateTime effectiveDate = date ?? DateTime.Today;
            var dateString = effectiveDate.ToString("yyyy/MM/dd");
            string url = apiName switch
            {
                "條件式篩選商品" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/product/pro_query.php?nowpage={pageNumber}&pagesize=100",
                "查詢庫存" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/inventory/stock_query.php?sku={skuString}",
                "條件式篩選訂單" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/order/order_query.php?nowpage={pageNumber}&pagesize=50&order_date={dateString}",
                _ => throw new ArgumentException("無效的 API 名稱", nameof(apiName))
            };

            if (!_cache.TryGetValue("AccessToken", out string accessToken))
            {
                _logger.LogWarning("快取中找不到存取令牌。");
                return null;
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API 呼叫失敗: {ReasonPhrase}", response.ReasonPhrase);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var pageDataResponse = new PageDataResponse();

                using (var doc = JsonDocument.Parse(content))
                {
                    var dataElement = doc.RootElement.GetProperty("data");
                    if (dataElement.ValueKind == JsonValueKind.Object)
                    {
                        var rowsElement = dataElement.GetProperty("rows");
                        if (rowsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var row in rowsElement.EnumerateArray())
                            {
                                if (apiName == "條件式篩選商品")
                                {
                                    var id = row.GetProperty("id").GetString();
                                    var name = row.GetProperty("name").GetString();
                                    pageDataResponse.Products ??= new List<WmsProduct>();
                                    pageDataResponse.Products.Add(new WmsProduct { Id = id, Name = name });
                                }
                                else if (apiName == "查詢庫存")
                                {
                                    var sku = row.GetProperty("sku").GetString();
                                    var stock = row.GetProperty("stock").GetInt32();
                                    var name = row.GetProperty("name").GetString();
                                    pageDataResponse.Products ??= new List<WmsProduct>();
                                    pageDataResponse.Products.Add(new WmsProduct { Id = sku, Name = name, Stock = stock });
                                }
                                else if (apiName == "條件式篩選訂單")
                                {
                                    var statusCode = row.GetProperty("status_code").GetString();
                                    var statusName = row.GetProperty("status_name").GetString();
                                    if (statusCode == "P" || statusCode == "F")
                                    {
                                        var productsElement = row.GetProperty("products");
                                        if (productsElement.ValueKind == JsonValueKind.Array)
                                        {
                                            foreach (var product in productsElement.EnumerateArray())
                                            {
                                                var itemsElement = product.GetProperty("items");
                                                if (itemsElement.ValueKind == JsonValueKind.Array)
                                                {
                                                    foreach (var item in itemsElement.EnumerateArray())
                                                    {
                                                        var sku = item.GetProperty("sku").GetString();
                                                        var qty = item.GetProperty("qty").GetInt32();
                                                        pageDataResponse.Orders ??= new List<WmsOrder>();
                                                        pageDataResponse.Orders.Add(new WmsOrder { status_code = statusCode, status_name = statusName, sku = sku, qty = qty });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (dataElement.TryGetProperty("maxpage", out var maxPageElement))
                        {
                            pageDataResponse.MaxPage = maxPageElement.GetInt32();
                        }
                    }
                }

                return pageDataResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError("發生 API 呼叫錯誤: {Message}", ex.Message);
                return null;
            }
        }

        public class PageDataResponse
        {
            public List<WmsProduct> Products { get; set; }
            public List<WmsOrder> Orders { get; set; }
            public int MaxPage { get; set; }
        }

        // 將撈到的商品 list，按照 50 筆拆分出字串
        public static List<string> GroupIds(List<WmsProduct> allData, int groupSize = 50)
        {
            return allData
                .Select(x => x.Id)
                .Select((id, index) => new { id, index })
                .GroupBy(x => x.index / groupSize)
                .Select(g => string.Join(",", g.Select(x => x.id)))
                .ToList();
        }

        public async Task<IActionResult> StoreNum()
        {
            var pdData = await GetProductDataAsync();
            pdData = await GetStockDataAsync(pdData);
            pdData = await GetOrderDataAsync(pdData);
            return View(pdData);
        }
    }
}
