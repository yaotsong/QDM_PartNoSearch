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
using DocumentFormat.OpenXml.Spreadsheet;

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

        // API判斷商品條件是否上下架
        public async Task<List<WmsProduct>> GetProductStatusAsync(List<WmsProduct> data)
        {
            
            if (data.Any())
            {
                var responsesList = new List<WmsProduct>();
                foreach (var item in data)
                {
                    var check = await GetPageDataAsync("查詢產品細節", 1, item.Id);
                    if (check.status)
                    {
                        responsesList.Add(item);
                    }
                }
                data = responsesList;
            }
            return data;
        }

        // API抓商品庫存用，並將庫存數字合併商品 list
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

            return responsesList;
        }

        // API 抓訂單資料用，並將訂單商品明細合併到商品 list
        public async Task<List<WmsProduct>> GetOrderDataAsync(List<WmsProduct> data, string market)
        {
            DateTime today = DateTime.Today;
            DateTime firstDayOfMonth = today.AddDays(-60); // 調整訂單起始天數，從前 60 天開始

            var dataDict = data.ToDictionary(item => $"{item.Id}-{item.Warehouse}");
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
                    if (string.IsNullOrEmpty(order.sku) || string.IsNullOrEmpty(order.warehouse))
                    {
                        _logger.LogWarning("訂單缺少 SKU 或 Warehouse: {OrderId}", order.sku);
                        continue; // 跳過這筆資料，避免將 null 的 sku 加入字典
                    }

                    var orderKey = $"{order.sku}-{order.warehouse}";

                    if (orderAllData.ContainsKey(orderKey))
                    {
                        var existing = orderAllData[orderKey];
                        // 更新數量和名稱（名稱保留最新的）
                        orderAllData[orderKey] = new Tuple<int, string>(existing.Item1 + order.qty, order.name);
                    }
                    else
                    {
                        // 新增 SKU+倉庫 和對應的數量及名稱
                        orderAllData[orderKey] = new Tuple<int, string>(order.qty, order.name);
                    }
                }

                firstDayOfMonth = firstDayOfMonth.AddDays(1);
            }
            while (firstDayOfMonth <= today);

            foreach (var kvp in orderAllData)
            {
                if (kvp.Key == null)
                {
                    _logger.LogWarning("Encountered a null SKU in orderAllData.");
                    continue; // 跳過這筆資料，避免將 null 的 sku 加入 dataDict
                }

                if (dataDict.TryGetValue(kvp.Key, out var existingItem))
                {
                    existingItem.Qty += kvp.Value.Item1;
                }
                else
                {
                    var keyParts = kvp.Key.Split('-');
                    var sku = keyParts[0];
                    var warehouse = keyParts.Length > 1 ? keyParts[1] : "未知倉庫";

                    dataDict[kvp.Key] = new WmsProduct
                    {
                        Id = sku,
                        Warehouse = warehouse,
                        Qty = kvp.Value.Item1,
                        Name = "(不在日翊庫存)" + kvp.Value.Item2
                    };
                }
            }

            return dataDict.Values.ToList();
        }



        //主要處理api端的呼叫並回傳資料
        private async Task<PageDataResponse> GetPageDataAsync(string apiName, int pageNumber, string skuString = "", DateTime? date = null)
        {
            DateTime effectiveDate = date ?? DateTime.Today;
            var dateString = effectiveDate.ToString("yyyy/MM/dd");
            string url = apiName switch
            {
                "條件式篩選商品" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/product/pro_query.php?nowpage={pageNumber}&pagesize=100",
                "查詢產品細節" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/product/pro_detail.php?sku={skuString}",
                "查詢庫存" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/inventory/stock_query.php?sku={skuString}",
                "日翊條件式篩選訂單" => $"https://reyi-distribution.wms.changliu.com.tw/api_v1/order/order_query.php?nowpage={pageNumber}&pagesize=50&order_date={dateString}&status=F&source_key=qdm,qdm_excel,hand,shopee", //status訂單狀態: F代處理 P已轉單 W轉單中
                "暢流條件式篩選訂單" => $"https://192.168.1.100/api_v1/order/order_query.php?nowpage={pageNumber}&pagesize=50&order_date={dateString}&status=F&source_key=qdm,qdm_excel,qdm_excel2",
                _ => throw new ArgumentException("無效的 API 名稱", nameof(apiName))
            };

            string? cacheKey = apiName switch
            {
                "條件式篩選商品" => "ReyiAccessToken",
                "查詢產品細節" => "ReyiAccessToken",
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
                //存取產品料號資料用
                var pageDataResponse = new PageDataResponse();

                using (var doc = JsonDocument.Parse(content))
                {
                    var dataElement = doc.RootElement.GetProperty("data");
                    if (dataElement.ValueKind == JsonValueKind.Object)
                    {
                        //查詢產品資料格式與其他api回傳格式不同,並且只要回傳ture(上架) or false (下架) 就好
                        if (apiName == "查詢產品細節")
                        {
                            var status = dataElement.GetProperty("status").GetBoolean();
                            pageDataResponse.status = status;
                            return pageDataResponse;
                        }
                        else
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
                                        //先紀錄料品名稱
                                        var sku = row.GetProperty("sku").GetString();   //商品編號
                                        var name = row.GetProperty("name").GetString(); //商品名稱
                                        var spacesElement = row.GetProperty("spaces");
                                        // 使用 Dictionary 來累加相同 SKU 與 wh_id 的可用庫存
                                        var warehouseStockMap = new Dictionary<(string, string), int>();
                                        if (spacesElement.ValueKind == JsonValueKind.Array)
                                        {
                                            foreach (var spaces in spacesElement.EnumerateArray())
                                            {
                                                var wh_id = spaces.GetProperty("wh_id").GetString(); //倉庫編號
                                                var stock = spaces.GetProperty("stock").GetInt32(); //庫存數
                                                var occupied_stock = spaces.GetProperty("occupied_stock").GetInt32(); //占用庫存
                                                // 計算可用庫存 = stock - occupied_stock
                                                var availableStock = stock - occupied_stock;
                                                var key = (sku, wh_id); // 使用 (sku, wh_id) 作為 Key，確保相同商品在同倉庫合併
                                                // 如果 Dictionary 已有相同 SKU + 倉庫，則累加庫存
                                                if (warehouseStockMap.ContainsKey(key))
                                                {
                                                    warehouseStockMap[key] += availableStock;
                                                }
                                                else
                                                {
                                                    warehouseStockMap[key] = availableStock;
                                                }
                                            }
                                            // 將累加結果加入 pageDataResponse.Products
                                            pageDataResponse.Products ??= new List<WmsProduct>();

                                            foreach (var kvp in warehouseStockMap)
                                            {
                                                var (skuKey, whKey) = kvp.Key; // 解構 Tuple 取得 SKU 和倉庫名稱
                                                pageDataResponse.Products.Add(new WmsProduct
                                                {
                                                    Id = sku,
                                                    Name = name,
                                                    Stock = kvp.Value,  // 這裡的 Stock 已經是 (stock - occupied_stock) 累加後的值
                                                    Warehouse = whKey
                                                });
                                            }

                                        }
                                    }
                                    else if (apiName == "日翊條件式篩選訂單" || apiName == "暢流條件式篩選訂單")
                                    {
                                        var source_key = row.GetProperty("source_key").GetString(); //來源的EC平台
                                        var statusCode = row.GetProperty("status_code").GetString(); //訂單狀態碼:F待處理/W轉單中/P轉揀貨
                                        var statusName = row.GetProperty("status_name").GetString();
                                        var order_no = row.GetProperty("order_no").GetString();

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
                                                            source_key = (source_key == "qdm" || source_key == "qdm_excel") ? "富味鄉-官網" : "富味鄉-蝦皮";
                                                            pageDataResponse.Orders ??= new List<WmsOrder>();
                                                            pageDataResponse.Orders.Add(new WmsOrder { warehouse = source_key, status_code = statusCode, status_name = statusName, sku = sku, name = name, qty = qty });
                                                        }
                                                        else
                                                        {
                                                            foreach (var item in itemsElement.EnumerateArray())
                                                            {
                                                                var itemSku = item.GetProperty("sku").GetString();
                                                                var itemQty = item.GetProperty("qty").GetInt32();
                                                                if (itemQty > 0) {
                                                                    var test = true;
                                                                } 
                                                                var itemName = item.GetProperty("name").GetString();
                                                                source_key = (source_key == "qdm" || source_key == "qdm_excel") ? "富味鄉-官網" : "富味鄉-蝦皮";
                                                                pageDataResponse.Orders ??= new List<WmsOrder>();
                                                                pageDataResponse.Orders.Add(new WmsOrder { warehouse = source_key, status_code = statusCode, status_name = statusName, sku = itemSku, name = itemName, qty = itemQty });
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //回寫api最大頁數
                            if (dataElement.TryGetProperty("maxpage", out var maxPageElement))
                            {
                                pageDataResponse.MaxPage = maxPageElement.GetInt32();
                            }
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
            public List<WmsProduct>? Products { get; set; }
            public List<WmsOrder>? Orders { get; set; }
            public int MaxPage { get; set; }
            public bool status {  get; set; }
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

        //主程式
        public async Task<IActionResult> StoreNum()
        {
            try
            {
                // 獲取商品資料
                var pdData = await GetProductDataAsync();
                // 判斷庫存狀態是否上下架
                pdData = await GetProductStatusAsync(pdData);
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
