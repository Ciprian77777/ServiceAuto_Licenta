using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    public class HomeController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;

        public HomeController(ServiceAutoLicentaContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var azi = DateTime.Today;
            var lunaAceasta = new DateTime(azi.Year, azi.Month, 1);

            ViewBag.TotalClienti = await _context.Clienti.CountAsync();
            ViewBag.TotalMasini = await _context.Masini.CountAsync();
            ViewBag.TotalProgramari = await _context.Programari.CountAsync();
            ViewBag.TotalPiese = await _context.Piese.CountAsync();

            ViewBag.ProgramariInLucru = await _context.Programari
                .CountAsync(p => p.Status == StatusProgramare.InLucru);
            ViewBag.ProgramariAzi = await _context.Programari
                .CountAsync(p => p.DataIntrare == azi);

            ViewBag.VenituriLuna = await _context.Facturi
                .Where(f => f.StatusPlata == StatusPlata.Platita &&
                            f.DataEmitere >= lunaAceasta)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            ViewBag.FacturiNeincasate = await _context.Facturi
                .CountAsync(f => f.StatusPlata == StatusPlata.Neplata);

            ViewBag.AlerteStoc = await _context.Piese
                .CountAsync(p => p.StocCurent <= p.StocMinim);

            ViewBag.UltimeProgramari = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.UltimiFacturi = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .OrderByDescending(f => f.DataEmitere)
                .Take(5)
                .ToListAsync();

            ViewBag.PieseStocScazut = await _context.Piese
                .Where(p => p.StocCurent <= p.StocMinim)
                .OrderBy(p => p.StocCurent)
                .Take(5)
                .ToListAsync();

            ViewBag.NrProgramata = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.Programata);
            ViewBag.NrInLucru = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.InLucru);
            ViewBag.NrFinalizata = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.Finalizata);
            ViewBag.NrAnulata = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.Anulata);

            
            var profitLunar = await _context.Facturi
                .Where(f => f.StatusPlata == StatusPlata.Platita && f.DataEmitere >= azi.AddMonths(-6))
                .GroupBy(f => new { f.DataEmitere.Year, f.DataEmitere.Month })
                .Select(g => new { An = g.Key.Year, Luna = g.Key.Month, Total = g.Sum(f => f.Total) })
                .OrderBy(x => x.An).ThenBy(x => x.Luna)
                .ToListAsync();

            ViewBag.ProfitLunarLabels = System.Text.Json.JsonSerializer.Serialize(profitLunar.Select(x => x.Luna + "/" + x.An).ToList());
            ViewBag.ProfitLunarDate = System.Text.Json.JsonSerializer.Serialize(profitLunar.Select(x => x.Total).ToList());

            return View();
        }
    }
}