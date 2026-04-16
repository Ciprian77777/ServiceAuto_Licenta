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
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var azi = DateTime.Today;
            var lunaAceasta = new DateTime(azi.Year, azi.Month, 1);

            ViewBag.TotalClienti = await _context.Clienti.CountAsync(c => c.UserId == userId);
            ViewBag.TotalMasini = await _context.Masini.CountAsync(m => m.Client.UserId == userId);
            ViewBag.TotalProgramari = await _context.Programari.CountAsync(p => p.Masina.Client.UserId == userId);
            ViewBag.TotalPiese = await _context.Piese.CountAsync(p => p.UserId == userId);

            ViewBag.ProgramariInLucru = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.InLucru);

            ViewBag.VenituriLuna = await _context.Facturi
                .Where(f => f.Programare.Masina.Client.UserId == userId &&
                            f.StatusPlata == StatusPlata.Platita &&
                            f.DataEmitere >= lunaAceasta)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            ViewBag.VenituriLunaAnt = await _context.Facturi
                .Where(f => f.Programare.Masina.Client.UserId == userId &&
                            f.StatusPlata == StatusPlata.Platita &&
                            f.DataEmitere >= lunaAceasta.AddMonths(-1) &&
                            f.DataEmitere < lunaAceasta)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            ViewBag.FacturiNeincasate = await _context.Facturi
                .CountAsync(f => f.Programare.Masina.Client.UserId == userId &&
                                 f.StatusPlata == StatusPlata.Neplata);

            ViewBag.AlerteStoc = await _context.Piese
                .CountAsync(p => p.UserId == userId && p.StocCurent <= p.StocMinim);

            ViewBag.NrProgramata = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.Programata);
            ViewBag.NrInLucru = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.InLucru);
            ViewBag.NrFinalizata = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.Finalizata);
            ViewBag.NrAnulata = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.Anulata);

            var profitLunar = await _context.Facturi
                .Where(f => f.Programare.Masina.Client.UserId == userId &&
                            f.StatusPlata == StatusPlata.Platita &&
                            f.DataEmitere >= azi.AddMonths(-6))
                .GroupBy(f => new { f.DataEmitere.Year, f.DataEmitere.Month })
                .Select(g => new { An = g.Key.Year, Luna = g.Key.Month, Total = g.Sum(f => f.Total) })
                .OrderBy(x => x.An).ThenBy(x => x.Luna)
                .ToListAsync();

            ViewBag.ProfitLunarLabels = System.Text.Json.JsonSerializer.Serialize(
                profitLunar.Select(x => x.Luna + "/" + x.An).ToList());
            ViewBag.ProfitLunarDate = System.Text.Json.JsonSerializer.Serialize(
                profitLunar.Select(x => x.Total).ToList());

            ViewBag.UltimeProgramari = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Where(p => p.Masina.Client.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5).ToListAsync();

            ViewBag.UltimiFacturi = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .Where(f => f.Programare.Masina.Client.UserId == userId)
                .OrderByDescending(f => f.DataEmitere)
                .Take(5).ToListAsync();

            ViewBag.PieseStocScazut = await _context.Piese
                .Where(p => p.UserId == userId && p.StocCurent <= p.StocMinim)
                .OrderBy(p => p.StocCurent)
                .Take(5).ToListAsync();

            ViewBag.AlerteGlobale = await _context.Piese
                .CountAsync(p => p.UserId == userId && p.StocCurent <= p.StocMinim);

            return View();
        }
    }
}