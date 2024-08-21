using Microsoft.AspNetCore.Mvc;
using QDM_PartNoSearch.Models;
using System.Diagnostics;

namespace QDM_PartNoSearch.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Flavor2Context _context;
        public HomeController(ILogger<HomeController> logger, Flavor2Context context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            //撈取INVMB裡的MB001跟MB002欄位
            var query = this._context.Invmbs.Select(x=> x.Mb002).AsQueryable();
            var partNo = query.ToList();
            ViewBag.PartNumbers = partNo;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult StoreNum()
        {
            return View();
        }

        //index頁面查詢料號
        public IActionResult GetPartNumbers(string q)
        {
            var MbQuery = _context.Invmbs.AsQueryable();
            var McQuery = _context.Invmcs.AsQueryable();

            var joinedQuery = from mb in MbQuery
                              join mc in McQuery
                              on mb.Mb001 equals mc.Mc001
                              where mc.Mc002 == "1531"
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
