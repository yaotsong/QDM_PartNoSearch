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

namespace QDM_PartNoSearch.Controllers
{
    public class QDMController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        public QDMController(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("NoCertValidationClient");
            _cache = cache;
        }
        // 定義模型以接收表單數據
        public class SearchModel
        {
            public DateTime? QueryDate { get; set; }
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

                // 呼叫 API 並生成 Excel 檔案
                var fileContent = await GetExcelData(date);

                // 返回 Excel 檔案下載
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "退貨單查詢結果.xlsx");
            }

            return View("Index", model);
        }
        public async Task<byte[]> GetExcelData(DateTime queryDate)
        {

            string orderUrl = "https://ecapis.qdm.cloud/api/v1/orders/return";
            _cache.TryGetValue("QDMAccessToken", out string? accessToken);
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
                throw new Exception($"API 請求失敗: {response.StatusCode}");
            }
        }
    }
}
