using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

        public IActionResult PredictPartNo()
        {
            //var invMB = _context.Invmbs
            //     .Where(x => x.Mb064 != 0)
            //     .Select(x => new
            //     {
            //         PartNo = x.Mb001.Trim(),
            //         Name = x.Mb002.Trim(),
            //         Num = x.Mb064
            //     })
            //     .ToList();

            // 返回視圖並傳遞資料
            return View(); // 確保你的視圖能夠正確顯示報告
        }

        public JsonResult GetPredictedPartNos(DateTime startDate, DateTime endDate)
        {
            // 撈取 PredictPartNo 的所有資料
            var predictPartNos = _deanContext.PredictPartNo
                .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
                .ToList();

            var invMB = _context.Invmbs
                .Where(x => x.Mb064 != 0)
                .Select(x => new
                {
                    Mb001 = x.Mb001.Trim(),
                    Mb002 = x.Mb002.Trim(),
                    Mb064 = x.Mb064
                })
                .ToList();

            // 儲存計算後庫存數量
            var formData = new List<object>();

            foreach (var inv in invMB)
            {
                // 檢查 inv.Mb001 是否在 predictPartNos 中
                var matchingPartNos = predictPartNos
                    .Where(p => p.PartNo == inv.Mb001 && DateTime.Now <= p.DateTime)
                    .ToList();

                // 計算加總
                var totalNum = matchingPartNos.Any() ? matchingPartNos.Sum(p => p.Num) : 0;

                // 創建新的條目並填充數據
                var newEntry = new
                {
                    PartNo = inv.Mb001,
                    Name = inv.Mb002,
                    Num = (int)inv.Mb064 + totalNum
                };

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
