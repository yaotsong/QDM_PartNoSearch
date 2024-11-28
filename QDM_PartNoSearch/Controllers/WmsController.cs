using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using QDM_PartNoSearch.Models;
using Microsoft.EntityFrameworkCore;

namespace QDM_PartNoSearch.Controllers
{
    public class WmsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WmsController> _logger;
        private readonly Flavor2Context _context;

        public WmsController(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<WmsController> logger, Flavor2Context context)
        {
            _httpClient = httpClientFactory.CreateClient("NoCertValidationClient");
            _cache = cache;
            _logger = logger;
            _context = context;
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
        public async Task<List<WmsProduct>> GetOrderDataAsync(List<WmsProduct> data, string market)
        {
            DateTime today = DateTime.Today;
            //DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1); //從當月1號開始
            DateTime firstDayOfMonth = today.AddDays(-60); //調整訂單起始天數 從前60天開始
            var dataDict = data.ToDictionary(item => item.Id);
            var orderAllData = new Dictionary<string, Tuple<int, string>>();

            do
            {
                int currentPage = 1;
                int maxPage;
                var orderList = new List<WmsOrder>();

                do
                {
                    PageDataResponse pageData = new PageDataResponse();
                    if (market == "日翊")
                    {
                        pageData = await GetPageDataAsync("日翊條件式篩選訂單", currentPage, "", firstDayOfMonth);
                    }
                    else if (market == "暢流")
                    {
                        pageData = await GetPageDataAsync("暢流條件式篩選訂單", currentPage, "", firstDayOfMonth);
                    }

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
                        var existing = orderAllData[order.sku];
                        // 更新數量和名稱（名稱保留最新的）
                        orderAllData[order.sku] = new Tuple<int, string>(existing.Item1 + order.qty, order.name);
                    }
                    else
                    {
                        // 新增 SKU 和對應的數量及名稱
                        orderAllData[order.sku] = new Tuple<int, string>(order.qty, order.name);
                    }
                }

                firstDayOfMonth = firstDayOfMonth.AddDays(1);
            }
            while (firstDayOfMonth <= today);

            foreach (var kvp in orderAllData)
            {
                if (dataDict.TryGetValue(kvp.Key, out var existingItem))
                {
                    existingItem.Qty += kvp.Value.Item1;
                }
                else
                {
                    dataDict[kvp.Key] = new WmsProduct { Id = kvp.Key, Qty = kvp.Value.Item1, Name = "(不在日翊庫存)" + kvp.Value.Item2 };
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
                "日翊條件式篩選訂單" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/order/order_query.php?nowpage={pageNumber}&pagesize=50&order_date={dateString}&status=P,F,W&source_key=qdm,qdm_excel,hand",
                "暢流條件式篩選訂單" => $"https://192.168.1.100/api_v1/order/order_query.php?nowpage={pageNumber}&pagesize=50&order_date={dateString}&status=P,F,W&source_key=qdm,qdm_excel,qdm_excel2",
                _ => throw new ArgumentException("無效的 API 名稱", nameof(apiName))
            };

            string? cacheKey = apiName switch
            {
                "條件式篩選商品" => "ReyiAccessToken",
                "查詢庫存" => "ReyiAccessToken",
                "日翊條件式篩選訂單" => "ReyiAccessToken",
                "暢流條件式篩選訂單" => "FlavorAccessToken",
                _ => null
            };

            if (cacheKey == null || !_cache.TryGetValue(cacheKey, out string? accessToken))
            {
                
                _logger.LogWarning("快取中找不到存取令牌:{AccessToken}。",cacheKey);
                return null;
            }
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("API呼叫成功:{Response}",accessToken);
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
                                    var sku = row.GetProperty("sku").GetString();   //商品編號
                                    var stock = row.GetProperty("stock").GetInt32(); //庫存數
                                    var name = row.GetProperty("name").GetString(); //商品名稱
                                    var occupied_stock = row.GetProperty("occupied_stock").GetInt32(); //占用庫存
                                    pageDataResponse.Products ??= new List<WmsProduct>();
                                    pageDataResponse.Products.Add(new WmsProduct { Id = sku, Name = name, Stock = stock });
                                }
                                else if (apiName == "日翊條件式篩選訂單" || apiName == "暢流條件式篩選訂單")
                                {
                                    var source_key = row.GetProperty("source_key").GetString(); //來源的EC平台
                                    var statusCode = row.GetProperty("status_code").GetString(); //訂單狀態碼:F待處理/W轉單中/P轉揀貨
                                    var statusName = row.GetProperty("status_name").GetString();

                                    var productsElement = row.GetProperty("products");
                                    if (productsElement.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var product in productsElement.EnumerateArray())
                                        {
                                            var productType = product.GetProperty("type").GetString(); // 商品類型
                                            var sku = product.GetProperty("sku").GetString();
                                            var qty = product.GetProperty("qty").GetInt32();
                                            var name = product.GetProperty("name").GetString();

                                            if (source_key == "hand") // 人工開單不用到items層
                                            {
                                                pageDataResponse.Orders ??= new List<WmsOrder>();
                                                pageDataResponse.Orders.Add(new WmsOrder { status_code = statusCode, status_name = statusName, sku = sku, name = name, qty = qty });
                                            }
                                            else
                                            {
                                                var itemsElement = product.GetProperty("items");

                                                if (itemsElement.ValueKind == JsonValueKind.Array)
                                                {
                                                    //原先要判斷有沒有展開料號，但發現暢流規則很難定義 放棄顯示未展開提示，只記錄為在日翊暢流的料
                                                    if (itemsElement.GetArrayLength() == 0 && (productType == "combine" || productType == "shop"))
                                                    {
                                                        pageDataResponse.Orders ??= new List<WmsOrder>();
                                                        pageDataResponse.Orders.Add(new WmsOrder { status_code = statusCode, status_name = statusName, sku = sku, name = name, qty = qty });
                                                    }
                                                    else
                                                    {
                                                        foreach (var item in itemsElement.EnumerateArray())
                                                        {
                                                            var itemSku = item.GetProperty("sku").GetString();
                                                            var itemQty = item.GetProperty("qty").GetInt32();
                                                            var itemName = item.GetProperty("name").GetString();
                                                            pageDataResponse.Orders ??= new List<WmsOrder>();
                                                            pageDataResponse.Orders.Add(new WmsOrder { status_code = statusCode, status_name = statusName, sku = itemSku, name = itemName, qty = itemQty });
                                                        }
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
                _logger.LogInformation("!!!!!!!!!統整資料成功!!!!!!!!!!!");
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

        public List<WmsProduct> MatchingPartNo (List<WmsProduct> allData)
        {
            var Invmhs = _context.Invmhs.ToList();
            foreach (var item in allData)
            {
                var matchingData = Invmhs.FirstOrDefault(x => x.MH002.Trim() == item.Id);
                if(matchingData != null)
                {
                    item.PartNo = matchingData.MH001.Trim();
                }
            }
            return allData;
        }

        public async Task<IActionResult> StoreNum()
        {
            try
            {
                // 獲取商品資料
                var pdData = await GetProductDataAsync();
                // 獲取庫存資料並更新 pdData
                pdData = await GetStockDataAsync(pdData);
                // 獲取訂單資料並更新 pdData
                pdData = await GetOrderDataAsync(pdData, "日翊");
                // 獲取訂單資料並更新 pdData
                pdData = await GetOrderDataAsync(pdData, "暢流"); 
                //將INVMH品號條碼對照表 合併到list裏頭
                pdData = MatchingPartNo(pdData);

                // 返回視圖，並將 pdData 作為模型傳遞給視圖
                return View(pdData);
            }
            catch (Exception ex)
            {
                _logger.LogError("錯誤: {Message}", ex.Message);
                // 返回錯誤視圖或處理錯誤的方式
                return View("Error");
            }
        }
    }
}
