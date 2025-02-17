using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Memory;
using QDM_PartNoSearch.Models;
using System.Text.RegularExpressions;
using Azure.Core;
using static QDM_PartNoSearch.Controllers.WmsController;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace QDM_PartNoSearch.Controllers
{
    public class QDMController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HomeController> _logger;
        private readonly Flavor2Context _context;

        public QDMController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache, Flavor2Context context)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("NoCertValidationClient");
            _cache = cache;
            _context = context;
        }
        // 定義模型以接收表單數據
        public class SearchModel
        {
            public DateTime? QueryDate { get; set; }
            public string? Pandin { get; set; }
        }
        //結合ERP銷貨單頭單身欄位
        public class CombinedCOPTGH
        {
            public Coptg CoptgData { get; set; }
            public Copth CopthData { get; set; }
            public RefundReyiOrderData reyiOrderData { get; set; }
        }
        //存取暢流撈到的訂單資料
        public class RefundReyiOrderData
        {
            public string OrderNo { get; set; }
            public string ProductName { get; set; }
            public int ProductPrice { get; set; }
            public int ProductQty { get; set; }
            public string InvoiceCode { get; set; }
            public string InvoiceDate { get; set; }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(SearchModel model)
        {
            if (model.QueryDate.HasValue)
            {
                var date = model.QueryDate.Value;
                var check = model.Pandin;
                byte[] fileContent = null;
                List<RefundReyiOrderData> reyiListData;
                List<CombinedCOPTGH> erpRefundData;
                switch (check)
                {
                    case "ATM":
                        fileContent = await GetQDMExcelData(date);
                        break;
                    case "ERP":
                        //取得日翊退貨單資料
                        reyiListData = await ReyiRefundData(date);
                        erpRefundData = erpPurchaseOrder(reyiListData,date, check);
                        fileContent = GetERPExcelData(erpRefundData); 
                        break;
                    case "綠界":
                        //取得日翊退貨單資料
                        reyiListData = await ReyiRefundData(date);
                        erpRefundData = erpPurchaseOrder(reyiListData, date, check);
                        fileContent = GetECpayExcelData(erpRefundData);
                        break;
                    default:
                        break;

                }

                // 如果沒有資料，回傳 BadRequest 或根據需求處理
                if (fileContent == null)
                {
                    _logger.LogDebug("EXCEL匯出未找到符合條件的資料。");
                    return BadRequest("未找到符合條件的資料，請返回上一頁。");
                }


                // 返回 Excel 檔案下載
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{check}退貨查詢結果.xlsx");
            }

            return View("Index", model);
        }
        
        //呼叫日翊暢流已退貨訂單資料
        public async Task<List<RefundReyiOrderData>> ReyiRefundData(DateTime queryDate)
        {
            //轉換日期格式
            var date = queryDate.ToString("yyyy/MM/dd");
            //撈取日翊暢流的退貨訂單
            var url = $"https://reyi-distribution.wms.changliu.com.tw/api_v1/order/order_query.php?nowpage=1&pagesize=20&status=R&source_key=qdm&order_date={date}";
            if (!_cache.TryGetValue("ReyiAccessToken", out string? accessToken))
            {
                _logger.LogError("快取中找不到存取令牌:ReyiAccessToken");
                return null;
            };
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("API呼叫成功:{Response}", accessToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API 呼叫失敗: {ReasonPhrase}", response.ReasonPhrase);
                    return null;
                }
                //存取退貨訂單單號
                List<RefundReyiOrderData> orderNoList = new List<RefundReyiOrderData>();
                var content = await response.Content.ReadAsStringAsync();
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
                                var orderData = row.GetProperty("order_no").GetString();
                                //只抓訂單號後10碼
                                var orderNo = orderData.Substring(orderData.Length - 10);
                                var productElement = row.GetProperty("products");
                                if (productElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var product in productElement.EnumerateArray())
                                    {
                                        var productName = product.GetProperty("name").GetString();
                                        var productPrice = product.GetProperty("price").GetInt32();
                                        var productQty = product.GetProperty("qty").GetInt32();
                                        orderNoList.Add(new RefundReyiOrderData { OrderNo = orderNo, ProductName = productName, ProductPrice = productPrice, ProductQty = productQty });  // 使用 Add 方法加入訂單號
                                    }
                                }
                            }
                        }
                        
                    }
                }
                return orderNoList;
            }
            catch (Exception e)
            {
                _logger.LogError("發生 API 呼叫錯誤: {Message}", e.Message);
                return null;
            }

        }
        public List<CombinedCOPTGH> erpPurchaseOrder(List<RefundReyiOrderData> list, DateTime querydate, string pandin)
        {
            var coptgQuery = _context.Coptgs.AsNoTracking().AsQueryable();
            var copthQuery = _context.Copths.AsNoTracking().AsQueryable();
            List<Coptg> choiceOrder = new List<Coptg>();
            //匯出綠界發票時一定要跟暢流賣出去的名稱一樣，所以直接抓綠界的訂單資料+ERP銷貨單的發票號碼就好
            if (pandin == "綠界")
            {
                List<RefundReyiOrderData> dicountData = new List<RefundReyiOrderData>();
                foreach (var item in list)
                {
                    var invoiceCode = coptgQuery
                        .Where(x => x.TG029 == item.OrderNo)
                        .Select(x => x.TG014)
                        .FirstOrDefault();
                    var invoiceDate = coptgQuery
                        .Where(x => x.TG029 == item.OrderNo)
                        .Select(x => x.TG021)
                        .FirstOrDefault();
                    //取得單別單號
                    var getTG001TG002 = coptgQuery
                        .Where(x => x.TG029 == item.OrderNo)
                        .Select(x => new { x.TG001, x.TG002 })
                        .FirstOrDefault();
                    var getDiscount = copthQuery
                                      .Where(x => x.TH001 == getTG001TG002.TG001 && x.TH002 == getTG001TG002.TG002 && x.TH005 == "銷貨折扣")
                                      .Select(x=> new { x.TH004, x.TH005, x.TH008, x.TH012 })
                                      .FirstOrDefault();
                    if (getDiscount != null)
                    {
                        // 創建 RefundReyiOrderData 物件，並賦值
                        RefundReyiOrderData discountlist = new RefundReyiOrderData
                        {
                            // 假設 RefundReyiOrderData 類型有對應的屬性，你需要根據實際情況進行對應賦值
                            InvoiceCode = invoiceCode,
                            InvoiceDate = invoiceDate,
                            ProductName = getDiscount.TH005,
                            ProductQty = Convert.ToInt32(getDiscount.TH008),
                            ProductPrice = Convert.ToInt32(getDiscount.TH012)
                        };
                        // 檢查 dicountData 是否已經包含相同的 InvoiceCode
                        if (!dicountData.Any(x => x.InvoiceCode == discountlist.InvoiceCode))
                        {
                            dicountData.Add(discountlist);  // 如果沒有相同的 InvoiceCode，則新增
                        }
                    }
                    
                    item.InvoiceCode = invoiceCode;
                    item.InvoiceDate = invoiceDate;
                }
                list.AddRange(dicountData);
                list = list.OrderBy(x => x.InvoiceCode).ToList();
                var result = (from info in list
                              select new CombinedCOPTGH
                              {
                                  reyiOrderData = info,
                              }).ToList();
                return result;
            }
            //比對銷貨單備註三欄位 = 暢流退貨訂單單號
            if (pandin == "ERP")
            {
                var hashSet = new HashSet<string>();
                foreach(var item in list)
                {
                    var orderNo = item.OrderNo;
                    hashSet.Add(orderNo);
                }
                //將銷貨單單頭記錄下來
                choiceOrder = coptgQuery
                .Where(x => hashSet.Contains(x.TG029))
                .ToList();
            
                //先比對銷貨單單頭備註三的訂單編號在join銷貨單單身
                if (!choiceOrder.IsNullOrEmpty()){
                    var result = (from order in choiceOrder
                                  join copth in copthQuery
                                  on new { TG001 = order.TG001, TG002 = order.TG002 }
                                  equals new { TG001 = copth.TH001, TG002 = copth.TH002 }
                                  select new CombinedCOPTGH
                                  {
                                      CoptgData = order,
                                      CopthData = copth
                                  }).ToList();
                    return result;
                }
            }
            return null;

            
        }
        //QDM退貨單匯出EXCEL
        public async Task<byte[]> GetQDMExcelData(DateTime queryDate)
        {
            string orderUrl = "https://ecapis.qdm.cloud/api/v1/orders/return";
            if (!_cache.TryGetValue("QDMAccessToken", out string? accessToken))
            {
                _logger.LogError("快取中找不到存取令牌:QDMAccessToken");
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var today = DateTime.Today;

            string createdAtMin = queryDate.ToString("yyyy-MM-dd");
            string createdAtMax = today.ToString("yyyy-MM-dd");

            var formData = new Dictionary<string, string>
            {
                { "created_at_min", createdAtMin },
                { "created_at_max", createdAtMax },
                { "status", "1" },
                { "payment_status", "PAID" }
            };

            var content = new FormUrlEncodedContent(formData);

            // 设置 Content - Type
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            // 強制 GET 請求帶有 Body
            var response = await _httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get, // 使用 GET 方法
                RequestUri = new Uri(orderUrl), // 請求 URL
                Content = content // 傳遞表單資料作為 Body
            });

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(responseContent);

                var result = jsonObject["data"]["result"];

                List<Dictionary<string, string>> extractedData = new List<Dictionary<string, string>>();

                foreach (var item in result)
                {
                    //拆分退貨明細裡的退貨銀行輸入欄位
                    var bank_Info = item["return_return_bank"].ToString();
                    var lines = bank_Info.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    // 預設變數
                    string bank_name = string.Empty;
                    string bank_branch = string.Empty;
                    string bank_account = string.Empty;
                    string bank_user = string.Empty;

                    // 抓取每行冒號後的文字並分配給相對應的變數
                    if (lines.Length >= 1)
                    {
                        var match1 = Regex.Match(lines[0], @"[：:]\s*([^．]+)");
                        if (match1.Success) bank_name = match1.Groups[1].Value;
                    }

                    if (lines.Length >= 2)
                    {
                        var match2 = Regex.Match(lines[1], @"[：:]\s*([^．]+)");
                        if (match2.Success) bank_branch = match2.Groups[1].Value;
                    }

                    if (lines.Length >= 3)
                    {
                        var match3 = Regex.Match(lines[2], @"[：:]\s*([^．]+)");
                        if (match3.Success) bank_account = match3.Groups[1].Value;
                    }

                    if (lines.Length >= 4)
                    {
                        var match4 = Regex.Match(lines[3], @"[：:]\s*([^．]+)");
                        if (match4.Success) bank_user = match4.Groups[1].Value;
                    }

                    if (item["payment_method"].ToString() == "ATM轉帳")
                    {
                        var orderInfo = new Dictionary<string, string>
                        {
                            { "order_id", item["order_id"].ToString() }, //訂單編號
                            { "total", item["total"].ToString() }, //訂單總金額
                            { "bank_id", "" },
                            { "bank_name", bank_name },
                            { "bank_branch", bank_branch },
                            { "bank_account", bank_account },
                            { "bank_user", bank_user },
                        };
                        extractedData.Add(orderInfo);
                    }
                }

                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Orders");

                worksheet.Cell(1, 1).Value = "單號";
                worksheet.Cell(1, 2).Value = "銀行代號";
                worksheet.Cell(1, 3).Value = "銀行";
                worksheet.Cell(1, 4).Value = "分行";
                worksheet.Cell(1, 5).Value = "銀行帳戶";
                worksheet.Cell(1, 6).Value = "戶名";
                worksheet.Cell(1, 7).Value = "金額";

                int row = 2;
                foreach (var data in extractedData)
                {
                    worksheet.Cell(row, 1).Value = data["order_id"];
                    worksheet.Cell(row, 2).Value = data["bank_id"];
                    worksheet.Cell(row, 3).Value = data["bank_name"];
                    worksheet.Cell(row, 4).Value = data["bank_branch"];
                    worksheet.Cell(row, 5).Value = data["bank_account"];
                    worksheet.Cell(row, 6).Value = data["bank_user"];
                    worksheet.Cell(row, 7).Value = data["total"];
                    row++;
                }

                worksheet.Column(1).Width = 12;
                worksheet.Column(2).Width = 10;
                worksheet.Column(3).Width = 25;
                worksheet.Column(4).Width = 12;
                worksheet.Column(5).Width = 25;
                worksheet.Column(6).Width = 12;
                worksheet.Column(7).Width = 80;

                using (var memoryStream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            else
            {
                _logger.LogError("匯出QDM退貨資料API 呼叫失敗: {ReasonPhrase}", response.ReasonPhrase);
                throw new Exception($"API 請求失敗: {response.StatusCode}");
            }
        }
        //匯出ERP退貨報表
        public byte[] GetERPExcelData(List<CombinedCOPTGH> data)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("退貨單");

            worksheet.Cell(1, 1).Value = "單據日期";
            worksheet.Cell(1, 2).Value = "銷貨單號";
            worksheet.Cell(1, 3).Value = "發票號碼";
            worksheet.Cell(1, 4).Value = "客戶代號";
            worksheet.Cell(1, 5).Value = "客戶簡稱";
            worksheet.Cell(1, 6).Value = "統一編號";
            worksheet.Cell(1, 7).Value = "品號";
            worksheet.Cell(1, 8).Value = "品名";
            worksheet.Cell(1, 9).Value = "庫別代號";
            worksheet.Cell(1, 10).Value = "銷貨數量";
            worksheet.Cell(1, 11).Value = "單價";
            worksheet.Cell(1, 12).Value = "本幣含稅銷貨金額";
            worksheet.Cell(1, 13).Value = "備註";
            worksheet.Cell(1, 14).Value = "批號";
            worksheet.Cell(1, 15).Value = "連絡電話(一)";
            worksheet.Cell(1, 16).Value = "網路訂單編號";
            worksheet.Cell(1, 17).Value = "客戶全名";
            worksheet.Cell(1, 18).Value = "收貨人";
            worksheet.Cell(1, 19).Value = "銷退原因代號";
            int row = 2;
            foreach (var info in data)
            {
                worksheet.Cell(row, 1).Value = info.CoptgData.TG003;
                worksheet.Cell(row, 2).Value = info.CoptgData.TG001 + "-" + info.CoptgData.TG002;
                worksheet.Cell(row, 3).Value = info.CoptgData.TG014;
                worksheet.Cell(row, 4).Value = info.CoptgData.TG004;
                worksheet.Cell(row, 5).Value = "電子商務-綠界暢流";
                worksheet.Cell(row, 6).Value = info.CoptgData.TG015;
                worksheet.Cell(row, 7).Value = info.CopthData.TH004;
                worksheet.Cell(row, 8).Value = info.CopthData.TH005;
                worksheet.Cell(row, 9).Value = "1531";
                worksheet.Cell(row, 10).Value = info.CopthData.TH008;
                worksheet.Cell(row, 11).Value = info.CopthData.TH012;
                worksheet.Cell(row, 12).Value = info.CopthData.TH013;
                worksheet.Cell(row, 13).Value = "";
                worksheet.Cell(row, 14).Value = info.CopthData.TH017;
                worksheet.Cell(row, 15).Value = info.CoptgData.TG106;
                worksheet.Cell(row, 16).Value = info.CoptgData.TG029;
                worksheet.Cell(row, 17).Value = info.CoptgData.TG007;
                worksheet.Cell(row, 18).Value = info.CoptgData.TG076;
                worksheet.Cell(row, 19).Value = "RZ";
                row++;
            }

            using (var memoryStream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
        }
        //匯出綠界格式
        public byte[] GetECpayExcelData(List<CombinedCOPTGH> data)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("退貨單");

            worksheet.Cell(1, 1).Value = "匯入單號";
            worksheet.Cell(1, 2).Value = "發票號碼";
            worksheet.Cell(1, 3).Value = "發票開立日期";
            worksheet.Cell(1, 4).Value = "課稅別";
            worksheet.Cell(1, 5).Value = "同意類型";
            worksheet.Cell(1, 6).Value = "通知類型";
            worksheet.Cell(1, 7).Value = "通知電子信箱";
            worksheet.Cell(1, 8).Value = "通知手機號碼";
            worksheet.Cell(1, 9).Value = "折讓原因";
            worksheet.Cell(1, 10).Value = "折讓單總價";
            worksheet.Cell(1, 11).Value = "商品名稱";
            worksheet.Cell(1, 12).Value = "折讓數量";
            worksheet.Cell(1, 13).Value = "商品單位";
            worksheet.Cell(1, 14).Value = "單價";
            int row = 2;
            var orderCount = 0;
            var SumPrice = 0m;
            var invoiceNumber = "";
            var countPrice = data
                            .GroupBy(x => x.reyiOrderData.InvoiceCode)
                            .Select(x=> new
                                    {
                                        invoiceNumber = x.Key,
                                        SumPrice = x.Sum(i=>i.reyiOrderData.ProductPrice)
                                    });
            foreach (var info in data)
            {
                if (info.reyiOrderData.InvoiceCode != invoiceNumber)
                {
                    orderCount++;
                    SumPrice = countPrice.Where(x => x.invoiceNumber == info.reyiOrderData.InvoiceCode).Select(x=>x.SumPrice).FirstOrDefault();
                }
                worksheet.Cell(row, 1).Value = orderCount;
                worksheet.Cell(row, 2).Value = info.reyiOrderData.InvoiceCode == invoiceNumber  ? "" : info.reyiOrderData.InvoiceCode;
                worksheet.Cell(row, 3).Value = info.reyiOrderData.InvoiceCode == invoiceNumber ? "" : DateTime.ParseExact(info.reyiOrderData.InvoiceDate,"yyyyMMdd",null).ToString("yyyy/M/d");
                worksheet.Cell(row, 4).Value = info.reyiOrderData.InvoiceCode == invoiceNumber ? "" : "1";
                worksheet.Cell(row, 5).Value = info.reyiOrderData.InvoiceCode == invoiceNumber ? "" : "O";
                worksheet.Cell(row, 6).Value = info.reyiOrderData.InvoiceCode == invoiceNumber ? "" : "E";
                worksheet.Cell(row, 7).Value = info.reyiOrderData.InvoiceCode == invoiceNumber ? "" : "ec005@flavor.com.tw";
                worksheet.Cell(row, 8).Value = "";
                worksheet.Cell(row, 9).Value = info.reyiOrderData.InvoiceCode == invoiceNumber ? "" : "退貨";
                worksheet.Cell(row, 10).Value = info.reyiOrderData.InvoiceCode == invoiceNumber ? "" : SumPrice;
                worksheet.Cell(row, 11).Value = info.reyiOrderData.ProductName;
                worksheet.Cell(row, 12).Value = info.reyiOrderData.ProductQty;
                worksheet.Cell(row, 13).Value = "個";
                worksheet.Cell(row, 14).Value = info.reyiOrderData.ProductPrice;
                row++;
                invoiceNumber = info.reyiOrderData.InvoiceCode;
            }

            using (var memoryStream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
        }

    }
}
