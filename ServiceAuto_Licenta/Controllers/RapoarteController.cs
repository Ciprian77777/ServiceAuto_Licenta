using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class RapoarteController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RapoarteController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context=context;
            _userManager=userManager;
        }

        private string GetUserId()
        {
            return _userManager.GetUserId(User);
        }

        public async Task<IActionResult> Index()
        {
            var userId=GetUserId();
            var azi=DateTime.Today;
            var primaZiLuna=new DateTime(azi.Year, azi.Month, 1);

            ViewBag.TotalClienti=await _context.Clienti.CountAsync(c=>c.UserId==userId);
            ViewBag.TotalProgramari=await _context.Programari.CountAsync(p=>p.Masina.Client.UserId==userId);

            ViewBag.NrProgramata=await _context.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.Programata);
            ViewBag.NrInLucru=await _context.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.InLucru);
            ViewBag.NrFinalizata=await _context.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.Finalizata);
            ViewBag.NrAnulata=await _context.Programari.CountAsync(p=>p.Masina.Client.UserId==userId&&p.Status==StatusProgramare.Anulata);

            ViewBag.NrPlatite=await _context.Facturi.CountAsync(f=>f.Programare.Masina.Client.UserId==userId&&f.StatusPlata==StatusPlata.Platita);
            ViewBag.NrNeplata=await _context.Facturi.CountAsync(f=>f.Programare.Masina.Client.UserId==userId&&f.StatusPlata==StatusPlata.Neplata);
            ViewBag.NrPartial=await _context.Facturi.CountAsync(f=>f.Programare.Masina.Client.UserId==userId&&f.StatusPlata==StatusPlata.Partial);

            ViewBag.VenituriLuna=await _context.Facturi
                .Where(f=>f.Programare.Masina.Client.UserId==userId&&f.StatusPlata==StatusPlata.Platita&&f.DataEmitere>=primaZiLuna)
                .SumAsync(f=>(decimal?)f.Total)??0;

            var facturiChart=await _context.Facturi
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

            var lucrariLista=await _context.Lucrari
                .Where(l=>l.Programare.Masina.Client.UserId==userId)
                .ToListAsync();

            var lucrariTop=lucrariLista
                .GroupBy(l=>l.Denumire)
                .Select(g=>new { Denumire=g.Key, Count=g.Count(), TotalManopera=g.Sum(l=>l.Manopera) })
                .OrderByDescending(x=>x.Count)
                .Take(5)
                .ToList();

            ViewBag.LucrariTopLabels="[\"" + string.Join("\",\"", lucrariTop.Select(x=>x.Denumire)) + "\"]";
            ViewBag.LucrariTopDate="[" + string.Join(",", lucrariTop.Select(x=>x.Count)) + "]";
            ViewBag.LucrariTop=lucrariTop;

            var lucrarePieseLista=await _context.LucrarePiese
                .Include(lp=>lp.Piesa)
                .Where(lp=>lp.Lucrare.Programare.Masina.Client.UserId==userId)
                .ToListAsync();

            ViewBag.PieseTop=lucrarePieseLista
                .GroupBy(lp=>lp.Piesa.Denumire)
                .Select(g=>new { Denumire=g.Key, TotalCantitate=g.Sum(lp=>lp.Cantitate), TotalValoare=g.Sum(lp=>lp.Cantitate * lp.PretUnitar) })
                .OrderByDescending(x=>x.TotalCantitate)
                .Take(5)
                .ToList();

            var programariClienti=await _context.Programari
                .Include(p=>p.Masina).ThenInclude(m=>m.Client)
                .Where(p=>p.Masina.Client.UserId==userId)
                .ToListAsync();

            ViewBag.ClientiTop=programariClienti
                .GroupBy(p=>p.Masina.Client.Nume + " " + p.Masina.Client.Prenume)
                .Select(g=>new { Nume=g.Key, NrProgramari=g.Count(), TotalCheltuit=g.Sum(p=>p.TotalCuTva) })
                .OrderByDescending(x=>x.NrProgramari)
                .Take(5)
                .ToList();

            return View();
        }
    }
}