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
            // ���� PredictPartNo ���Ҧ����
            var predictPartNos = _deanContext.PredictPartNo.ToList();
            var invMB = _context.Invmbs.Select(x => new Invmb
            {
                Mb001 = x.Mb001.Trim(),
                Mb002 = x.Mb002.Trim(),
                Mb064 = x.Mb064
            }).Where(x=>x.Mb064 != 0).ToList();

            var report = GenerateInventoryReport(predictPartNos, invMB);
            // ��^���Ϩöǻ����
            return View(report); // �T�O�A�����ϯ�����T��ܳ��i
        }

        private Dictionary<string, List<DailyInventory>> GenerateInventoryReport(
            List<PredictPartNo> forecasts,
            List<Invmb> inventories)
        {
            var dailyInventory = new Dictionary<string, List<DailyInventory>>();

            // ��l�Ʈw�s
            foreach (var inventory in inventories)
            {
                dailyInventory[inventory.Mb001] = new List<DailyInventory>();
            }

            // ����Ҧ��w�����
            var allDates = forecasts.Select(f => f.DateTime.Date).Distinct().OrderBy(d => d).ToList();

            foreach (var date in allDates)
            {
                foreach (var inventory in inventories)
                {
                    int stock = (int)inventory.Mb064.GetValueOrDefault(); // �ϥ� GetValueOrDefault

                    // �֥[�w���ƶq
                    var forecastQuantity = forecasts
                        .Where(f => f.PartNo == inventory.Mb001 && f.DateTime.Date == date)
                        .Sum(f => f.Num.GetValueOrDefault()); // �T�O�w���ϥ�

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

        // index�����d�߮Ƹ�
        public IActionResult GetPartNumbers(string q)
        {
            var mbQuery = _context.Invmbs.AsQueryable();
            var mcQuery = _context.Invmcs.AsQueryable();

            var joinedQuery = from mb in mbQuery
                              join mc in mcQuery
                              on mb.Mb001 equals mc.Mc001
                              where mc.Mc002 == "1531" //�w�O1531
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
