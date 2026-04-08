using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    public class RapoarteController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;

        public RapoarteController(ServiceAutoLicentaContext context)
        {
            _context = context;
        }

        // GET: /Rapoarte
        public async Task<IActionResult> Index()
        {
            var azi = DateTime.Today;
            var lunaAceasta = new DateTime(azi.Year, azi.Month, 1);
            var lunaAnterioară = lunaAceasta.AddMonths(-1);

            // Venituri luna aceasta
            var venituriLuna = await _context.Facturi
                .Where(f => f.StatusPlata == StatusPlata.Platita &&
                            f.DataEmitere >= lunaAceasta)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            // Venituri luna anterioară
            var venituriLunaAnt = await _context.Facturi
                .Where(f => f.StatusPlata == StatusPlata.Platita &&
                            f.DataEmitere >= lunaAnterioară &&
                            f.DataEmitere < lunaAceasta)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            // Total programări luna aceasta
            var programariLuna = await _context.Programari
                .CountAsync(p => p.DataIntrare >= lunaAceasta);

            // Programări în lucru
            var inLucru = await _context.Programari
                .CountAsync(p => p.Status == StatusProgramare.InLucru);

            // Piese cu stoc scăzut
            var stocScazut = await _context.Piese
                .CountAsync(p => p.StocCurent <= p.StocMinim);

            // Facturi neîncasate
            var facturiNeincasate = await _context.Facturi
                .Where(f => f.StatusPlata == StatusPlata.Neplata)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            // Profit lunar (ultimele 6 luni)
            var profitLunar = await _context.Facturi
                .Where(f => f.StatusPlata == StatusPlata.Platita &&
                f.DataEmitere >= azi.AddMonths(-6))
                .GroupBy(f => new { f.DataEmitere.Year, f.DataEmitere.Month })
                .Select(g => new
                {
                    An = g.Key.Year,
                    Luna = g.Key.Month,
                    Total = g.Sum(f => f.Total)
                })
                .OrderBy(x => x.An).ThenBy(x => x.Luna)
                .ToListAsync();

            // Lucrări frecvente (top 5)
            var lucrariTop = await _context.Lucrari
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

            // Piese cel mai des utilizate (top 5)
            var pieseTop = await _context.LucrarePiese
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

            // Clienți cu cele mai multe programări (top 5)
            var clientiTop = await _context.Programari
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

            ViewBag.VenituriLuna = venituriLuna;
            ViewBag.VenituriLunaAnt = venituriLunaAnt;
            ViewBag.ProgramariLuna = programariLuna;
            ViewBag.InLucru = inLucru;
            ViewBag.StocScazut = stocScazut;
            ViewBag.FacturiNeincasate = facturiNeincasate;

            // Date pentru grafice (JSON pentru Chart.js)
            ViewBag.ProfitLunarLabels = System.Text.Json.JsonSerializer.Serialize(
                profitLunar.Select(x => x.Luna + "/" + x.An).ToList());
            ViewBag.ProfitLunarDate = System.Text.Json.JsonSerializer.Serialize(
                profitLunar.Select(x => x.Total).ToList());

            ViewBag.LucrariTopLabels = System.Text.Json.JsonSerializer.Serialize(
                lucrariTop.Select(x => x.Denumire).ToList());
            ViewBag.LucrariTopDate = System.Text.Json.JsonSerializer.Serialize(
                lucrariTop.Select(x => x.Count).ToList());

            ViewBag.PieseTop = pieseTop;
            ViewBag.ClientiTop = clientiTop;
            ViewBag.LucrariTop = lucrariTop;
            ViewBag.TotalClienti = await _context.Clienti.CountAsync();
            ViewBag.TotalProgramari = await _context.Programari.CountAsync();

            ViewBag.NrProgramata = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.Programata);
            ViewBag.NrInLucru = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.InLucru);
            ViewBag.NrFinalizata = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.Finalizata);
            ViewBag.NrAnulata = await _context.Programari.CountAsync(p => p.Status == StatusProgramare.Anulata);

            ViewBag.NrPlatite = await _context.Facturi.CountAsync(f => f.StatusPlata == StatusPlata.Platita);
            ViewBag.NrNeplata = await _context.Facturi.CountAsync(f => f.StatusPlata == StatusPlata.Neplata);
            ViewBag.NrPartial = await _context.Facturi.CountAsync(f => f.StatusPlata == StatusPlata.Partial);

            return View();
        }
    }
}