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
            // ���� PredictPartNo ���Ҧ����
            var predictPartNos = _deanContext.PredictPartNo
                .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
                .ToList();

            var invMC = (from mc in _context.Invmcs
                         join mb in _context.Invmbs
                         on mc.Mc001 equals mb.Mb001  // �ھ� Mc001 �M Mb001 �i�� join
                         where mc.Mc007 != 0  // �L�o����
                         select new
                         {
                             Mc001 = mc.Mc001.Trim(),  // �h�� �Ƹ� �r�Ŧꪺ�Ů�
                             Mc002 = mc.Mc002.Trim(),  // �h�� �w�O �r�Ŧꪺ�Ů�
                             Mc007 = mc.Mc007,          //�w�s�ƶq
                             Mb002 = mb.Mb002         // �ƦW
                         }).ToList();

            // �x�s�p���w�s�ƶq
            var formData = new List<object>();

            foreach (var inv in invMC)
            {
                // �p��ǰt���ƶq�`�M�A�ϥ� LINQ �����p���`�M
                var totalNum = predictPartNos
                    .Where(p => p.PartNo == inv.Mc001
                                && DateTime.Now.Date <= p.DateTime // �L�o�X�w�p�X�f����j�󵥩󤵤Ѫ��O��
                                && p.StockNum == inv.Mc002) // �ǰt�w�O
                    .Sum(x => x.Num.GetValueOrDefault());  // �ϥ� GetValueOrDefault() �ӽT�O�B�z null

                // �Ыطs�����بö�R�ƾ�
                var newEntry = new
                {
                    StockNum = inv.Mc002,
                    PartNo = inv.Mc001,
                    Name = inv.Mb002,
                    Num = inv.Mc007 + totalNum // �[�W��l�ƶq�M�p�⪺�`�M
                };

                // �N�s�����زK�[�� formData ���X
                formData.Add(newEntry);
            }
            var Data = new { data = formData };
            // ��^ JSON �榡�����
            return Json(Data);
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
                                  MB002 = mb.Mb002
                              };
            var result = joinedQuery.ToList();  // �T�O����d��
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
