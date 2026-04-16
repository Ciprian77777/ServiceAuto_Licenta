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
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var azi = DateTime.Today;
            var lunaAceasta = new DateTime(azi.Year, azi.Month, 1);

            // Venituri luna aceasta
            ViewBag.VenituriLuna = await _context.Facturi
                .Where(f => f.Programare.Masina.Client.UserId == userId &&
                            f.StatusPlata == StatusPlata.Platita &&
                            f.DataEmitere >= lunaAceasta)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            ViewBag.TotalClienti = await _context.Clienti.CountAsync(c => c.UserId == userId);
            ViewBag.TotalProgramari = await _context.Programari.CountAsync(p => p.Masina.Client.UserId == userId);

            // Statusuri programari
            ViewBag.NrProgramata = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.Programata);
            ViewBag.NrInLucru = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.InLucru);
            ViewBag.NrFinalizata = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.Finalizata);
            ViewBag.NrAnulata = await _context.Programari
                .CountAsync(p => p.Masina.Client.UserId == userId && p.Status == StatusProgramare.Anulata);

            // Statusuri facturi
            ViewBag.NrPlatite = await _context.Facturi
                .CountAsync(f => f.Programare.Masina.Client.UserId == userId && f.StatusPlata == StatusPlata.Platita);
            ViewBag.NrNeplata = await _context.Facturi
                .CountAsync(f => f.Programare.Masina.Client.UserId == userId && f.StatusPlata == StatusPlata.Neplata);
            ViewBag.NrPartial = await _context.Facturi
                .CountAsync(f => f.Programare.Masina.Client.UserId == userId && f.StatusPlata == StatusPlata.Partial);

            // Profit lunar (ultimele 6 luni)
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

            // Lucrari frecvente (top 5)
            var lucrariTop = await _context.Lucrari
                .Where(l => l.Programare.Masina.Client.UserId == userId)
                .GroupBy(l => l.Denumire)
                .Select(g => new
                {
                    Denumire = g.Key,
                    Count = g.Count(),
                    TotalManopera = g.Sum(l => l.Manopera)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            ViewBag.LucrariTopLabels = System.Text.Json.JsonSerializer.Serialize(
                lucrariTop.Select(x => x.Denumire).ToList());
            ViewBag.LucrariTopDate = System.Text.Json.JsonSerializer.Serialize(
                lucrariTop.Select(x => x.Count).ToList());
            ViewBag.LucrariTop = lucrariTop;

            // Piese top 5
            ViewBag.PieseTop = await _context.LucrarePiese
                .Where(lp => lp.Lucrare.Programare.Masina.Client.UserId == userId)
                .Include(lp => lp.Piesa)
                .GroupBy(lp => lp.Piesa.Denumire)
                .Select(g => new
                {
                    Denumire = g.Key,
                    TotalCantitate = g.Sum(lp => lp.Cantitate),
                    TotalValoare = g.Sum(lp => lp.Cantitate * lp.PretUnitar)
                })
                .OrderByDescending(x => x.TotalCantitate)
                .Take(5)
                .ToListAsync();

            // Clienti top 5
            ViewBag.ClientiTop = await _context.Programari
                .Where(p => p.Masina.Client.UserId == userId)
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .GroupBy(p => p.Masina.Client.Nume + " " + p.Masina.Client.Prenume)
                .Select(g => new
                {
                    Nume = g.Key,
                    NrProgramari = g.Count(),
                    TotalCheltuit = g.Sum(p => p.TotalCuTva)
                })
                .OrderByDescending(x => x.NrProgramari)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}