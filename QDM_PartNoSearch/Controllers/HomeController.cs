using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QDM_PartNoSearch.Models;
using System.Collections.Immutable;
using System.Diagnostics;

namespace QDM_PartNoSearch.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Flavor2Context _context;
        private readonly DeanContext _deanContext;

        public HomeController(ILogger<HomeController> logger, Flavor2Context context, DeanContext deanContext)
        {
            _logger = logger;
            _context = context;
            _deanContext = deanContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult HomePage()
        {
            return View();
        }

        public IActionResult PredictPartNo()
        {
            return View(); 
        }

        public JsonResult GetPredictedPartNos(DateTime startDate, DateTime endDate)
        {
            // 撈取 PredictPartNo 的所有資料
            var predictPartNos = _deanContext.PredictPartNo
                .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
                .ToList();

            var invMC = (from mc in _context.Invmcs
                         join mb in _context.Invmbs
                         on mc.Mc001 equals mb.Mb001  // 根據 Mc001 和 Mb001 進行 join
                         where mc.Mc007 != 0  // 過濾條件
                         select new
                         {
                             Mc001 = mc.Mc001.Trim(),  // 去除 料號 字符串的空格
                             Mc002 = mc.Mc002.Trim(),  // 去除 庫別 字符串的空格
                             Mc007 = mc.Mc007,          //庫存數量
                             Mb002 = mb.Mb002         // 料名
                         }).ToList();

            // 儲存計算後庫存數量
            var formData = new List<object>();

            foreach (var inv in invMC)
            {
                // 計算匹配的數量總和，使用 LINQ 直接計算總和
                var totalNum = predictPartNos
                    .Where(p => p.PartNo == inv.Mc001
                                && DateTime.Now.Date <= p.DateTime // 過濾出預計出貨日期大於等於今天的記錄
                                && p.StockNum == inv.Mc002) // 匹配庫別
                    .Sum(x => x.Num.GetValueOrDefault());  // 使用 GetValueOrDefault() 來確保處理 null

                // 創建新的條目並填充數據
                var newEntry = new
                {
                    StockNum = inv.Mc002,
                    PartNo = inv.Mc001,
                    Name = inv.Mb002,
                    Num = inv.Mc007 + totalNum // 加上原始數量和計算的總和
                };

                // 將新的條目添加到 formData 集合
                formData.Add(newEntry);
            }
            var Data = new { data = formData };
            // 返回 JSON 格式的資料
            return Json(Data);
        }


        public class DailyInventory
        {
            public DateTime Date { get; set; }
            public string PartNo { get; set; }
            public int Quantity { get; set; }
        }

        // index頁面查詢料號
        public IActionResult GetPartNumbers(string q)
        {
            var mbQuery = _context.Invmbs.AsQueryable();
            var mcQuery = _context.Invmcs.AsQueryable();

            var joinedQuery = from mb in mbQuery
                              join mc in mcQuery
                              on mb.Mb001 equals mc.Mc001
                              where mc.Mc002 == "1531" //庫別1531
                              select new
                              {
                                  MB001 = mb.Mb001,
                                  MB002 = mb.Mb002
                              };
            var result = joinedQuery.ToList();  // 確保執行查詢
            if (!string.IsNullOrEmpty(q))
            {
                joinedQuery = joinedQuery.Where(x => x.MB001.Contains(q) || x.MB002.Contains(q));
            }

            var results = joinedQuery.Select(x => new
            {
                id = x.MB001,
                text = $"{x.MB002}"
            }).Take(10).ToList();

            return Json(results);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
