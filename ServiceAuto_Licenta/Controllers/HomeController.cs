using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ServiceAutoLicentaContext db;
        private readonly UserManager<IdentityUser> userManager;

        public HomeController(ServiceAutoLicentaContext _db, UserManager<IdentityUser> _userManager)
        {
            db=_db;
            userManager=_userManager;
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
        public async Task<IActionResult> Index()
        {
            string? userId=userManager.GetUserId(User);
            var azi=DateTime.Today;
            var primaZiLuna=new DateTime(azi.Year, azi.Month, 1);
            var primaZiLunaAnt=primaZiLuna.AddMonths(-1);

            ViewBag.TotalClienti=await db.Clienti.CountAsync(x=>x.UserId==userId);
            ViewBag.TotalMasini=await db.Masini.CountAsync(x=>x.Client.UserId==userId);

            ViewBag.VenituriLuna=await db.Facturi
                .Where(x=>x.Programare.Masina.Client.UserId==userId&&x.StatusPlata==StatusPlata.Platita&&x.DataEmitere>=primaZiLuna)
                .SumAsync(x=>(decimal?)x.Total)??0;

            ViewBag.VenituriLunaAnt=await db.Facturi
                .Where(x=>x.Programare.Masina.Client.UserId==userId&&x.StatusPlata==StatusPlata.Platita&&x.DataEmitere>=primaZiLunaAnt&&x.DataEmitere<primaZiLuna)
                .SumAsync(x=>(decimal?)x.Total)??0;

            ViewBag.ProgramariInLucru=await db.Programari
                .CountAsync(x=>x.Masina.Client.UserId==userId&&x.Status==StatusProgramare.InLucru);

            ViewBag.NrProgramata=await db.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.Programata);
            ViewBag.NrInLucru=await db.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.InLucru);
            ViewBag.NrFinalizata=await db.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.Finalizata);
            ViewBag.NrAnulata=await db.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.Anulata);

            var facturiChart=await db.Facturi
                .Where(f=>f.Programare.Masina.Client.UserId==userId&&f.StatusPlata==StatusPlata.Platita&&f.DataEmitere>=azi.AddMonths(-6))
                .ToListAsync();

            var lblVenituri=new List<string>();
            var dtVenituri=new List<decimal>();
            for(int i=5; i>=0; i--)
            {
                var luna=azi.AddMonths(-i);
                decimal totalLuna=0;
                foreach(var f in facturiChart)
                {
                    if(f.DataEmitere.Year==luna.Year&&f.DataEmitere.Month==luna.Month)
                        totalLuna+=f.Total;
                }
                lblVenituri.Add(luna.Month + "/" + luna.Year);
                dtVenituri.Add(totalLuna);
            }
            ViewBag.ProfitLunarLabels="[\"" + string.Join("\",\"", lblVenituri) + "\"]";
            ViewBag.ProfitLunarDate="[" + string.Join(",", dtVenituri.Select(v=>((int)v).ToString())) + "]";

            var alerteStoc=await db.Piese.CountAsync(p=>p.UserId==userId&&p.StocCurent<=p.StocMinim);
            ViewBag.AlerteStoc=alerteStoc;
            ViewBag.AlerteGlobale=alerteStoc;
            ViewBag.PieseStocScazut=await db.Piese
                .Where(p=>p.UserId==userId&&p.StocCurent<=p.StocMinim)
                .Take(5)
                .ToListAsync();

            ViewBag.UltimeProgramari=await db.Programari
                .Include(x=>x.Masina).ThenInclude(x=>x.Client)
                .Where(x=>x.Masina.Client.UserId==userId)
                .OrderByDescending(x=>x.DataIntrare)
                .Take(5)
                .ToListAsync();

            ViewBag.UltimiFacturi=await db.Facturi
                .Include(x=>x.Programare).ThenInclude(x=>x.Masina).ThenInclude(x=>x.Client)
                .Where(x=>x.Programare.Masina.Client.UserId==userId)
                .OrderByDescending(x=>x.DataEmitere)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}