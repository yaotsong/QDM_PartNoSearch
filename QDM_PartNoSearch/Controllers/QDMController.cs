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

            var today = queryDate;

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
                    if (item["payment_method"].ToString() == "ATM轉帳")
                    {
                        var orderInfo = new Dictionary<string, string>
                        {
                            { "order_id", item["order_id"].ToString() },
                            { "payment_method", item["payment_method"].ToString() },
                            { "date_added", item["date_added"].ToString() },
                            { "return_name", item["return_name"].ToString() },
                            { "return_email", item["return_email"].ToString() },
                            { "return_telephone", item["return_telephone"].ToString() },
                            { "return_return_bank", item["return_return_bank"].ToString() }
                        };
                        extractedData.Add(orderInfo);
                    }
                }

                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Orders");

                worksheet.Cell(1, 1).Value = "訂單編號";
                worksheet.Cell(1, 2).Value = "付款方式";
                worksheet.Cell(1, 3).Value = "購買日期";
                worksheet.Cell(1, 4).Value = "姓名";
                worksheet.Cell(1, 5).Value = "電子郵件";
                worksheet.Cell(1, 6).Value = "電話";
                worksheet.Cell(1, 7).Value = "收貨地址";

                int row = 2;
                foreach (var data in extractedData)
                {
                    worksheet.Cell(row, 1).Value = data["order_id"];
                    worksheet.Cell(row, 2).Value = data["payment_method"];
                    worksheet.Cell(row, 3).Value = data["date_added"];
                    worksheet.Cell(row, 4).Value = data["return_name"];
                    worksheet.Cell(row, 5).Value = data["return_email"];
                    worksheet.Cell(row, 6).Value = data["return_telephone"];
                    worksheet.Cell(row, 7).Value = data["return_return_bank"];
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
