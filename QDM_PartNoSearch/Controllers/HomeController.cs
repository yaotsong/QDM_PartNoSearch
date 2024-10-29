using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QDM_PartNoSearch.Models;
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

        public IActionResult OrderList()
        {
            // 撈取 PredictPartNo 的所有資料
            var predictPartNos = _deanContext.PredictPartNo.ToList();
            var invMB = _context.Invmbs.Select(x => new Invmb
            {
                Mb001 = x.Mb001.Trim(),
                Mb002 = x.Mb002.Trim(),
                Mb064 = x.Mb064
            }).Where(x=>x.Mb064 != 0).ToList();

            var report = GenerateInventoryReport(predictPartNos, invMB);
            // 返回視圖並傳遞資料
            return View(report); // 確保你的視圖能夠正確顯示報告
        }

        private Dictionary<string, List<DailyInventory>> GenerateInventoryReport(
            List<PredictPartNo> forecasts,
            List<Invmb> inventories)
        {
            var dailyInventory = new Dictionary<string, List<DailyInventory>>();

            // 初始化庫存
            foreach (var inventory in inventories)
            {
                dailyInventory[inventory.Mb001] = new List<DailyInventory>();
            }

            // 獲取所有預估日期
            var allDates = forecasts.Select(f => f.DateTime.Date).Distinct().OrderBy(d => d).ToList();

            foreach (var date in allDates)
            {
                foreach (var inventory in inventories)
                {
                    int stock = (int)inventory.Mb064.GetValueOrDefault(); // 使用 GetValueOrDefault

                    // 累加預估數量
                    var forecastQuantity = forecasts
                        .Where(f => f.PartNo == inventory.Mb001 && f.DateTime.Date == date)
                        .Sum(f => f.Num.GetValueOrDefault()); // 確保安全使用

                    stock += forecastQuantity;

                    dailyInventory[inventory.Mb001].Add(new DailyInventory
                    {
                        Date = date,
                        PartNo = inventory.Mb001,
                        Quantity = stock
                    });
                }
            }

            return dailyInventory;
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
                                  MB002 = mb.Mb002,
                                  MC001 = mc.Mc001
                              };

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
