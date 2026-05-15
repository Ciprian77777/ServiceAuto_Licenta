using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceAutoLicenta.Data;
using ServiceAutoLicenta.Models.Entities;

namespace ServiceAutoLicenta.Controllers
{
    [Authorize]
    public class FacturiController : Controller
    {
        private readonly ServiceAutoLicentaContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FacturiController(ServiceAutoLicentaContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index(string? cautare, string? status)
        {
            var userId = GetUserId();
            var facturi = _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .Where(f => f.Programare.Masina.Client.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cautare))
            {
                cautare = cautare.ToLower();
                facturi = facturi.Where(f =>
                    f.SerieNumar.ToLower().Contains(cautare) ||
                    f.Programare.Masina.Client.Nume.ToLower().Contains(cautare) ||
                    f.Programare.Masina.Client.Prenume.ToLower().Contains(cautare) ||
                    f.Programare.Masina.NrInmatriculare.ToLower().Contains(cautare));
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusPlata>(status, out var statusEnum))
                facturi = facturi.Where(f => f.StatusPlata == statusEnum);

            ViewBag.Cautare = cautare;
            ViewBag.Status = status;
            ViewBag.TotalIncasat = await _context.Facturi
                .Where(f => f.Programare.Masina.Client.UserId == userId && f.StatusPlata == StatusPlata.Platita)
                .SumAsync(f => (decimal?)f.Total) ?? 0;

            return View(await facturi.OrderByDescending(f => f.DataEmitere).ToListAsync());
        }

        public async Task<IActionResult> Detalii(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .Include(f => f.Programare).ThenInclude(p => p.Lucrari).ThenInclude(l => l.LucrarePiese).ThenInclude(lp => lp.Piesa)
                .FirstOrDefaultAsync(f => f.Id == id && f.Programare.Masina.Client.UserId == GetUserId());

            if (factura == null) return NotFound();

            return View(factura);
        }

        public async Task<IActionResult> Genereaza(int? programareId)
        {
            if (programareId == null) return NotFound();

            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Include(p => p.Lucrari).ThenInclude(l => l.LucrarePiese).ThenInclude(lp => lp.Piesa)
                .Include(p => p.Factura)
                .FirstOrDefaultAsync(p => p.Id == programareId && p.Masina.Client.UserId == GetUserId());

            if (programare == null) return NotFound();

            if (programare.Factura != null)
            {
                TempData["Eroare"] = "Aceasta programare are deja o factura!";
                return RedirectToAction(nameof(Detalii), new { id = programare.Factura.Id });
            }

            var ultimaFactura = await _context.Facturi
                .Where(f => f.Programare.Masina.Client.UserId == GetUserId())
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            int numarNou = (ultimaFactura?.Id ?? 0) + 1;
            string serieNumar = $"SA-{DateTime.Now.Year}-{numarNou:D4}";

            var factura = new Factura
            {
                ProgramareId = programare.Id,
                SerieNumar = serieNumar,
                DataEmitere = DateTime.Today,
                DataScadenta = DateTime.Today.AddDays(30),
                StatusPlata = StatusPlata.Neplata,
                Subtotal = programare.TotalFaraTva,
                TvaValoare = programare.Tva,
                Total = programare.TotalCuTva
            };

            ViewBag.Programare = programare;
            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Genereaza([Bind("ProgramareId,SerieNumar,DataEmitere,DataScadenta,StatusPlata,MetodaPlata,Subtotal,TvaValoare,Total")] Factura factura)
        {
            ModelState.Remove("Programare");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(factura);
                    await _context.SaveChangesAsync();
                    TempData["Succes"] = $"Factura {factura.SerieNumar} a fost generata!";
                    return RedirectToAction(nameof(Detalii), new { id = factura.Id });
                }
                catch (DbUpdateException)
                {
                    TempData["Eroare"] = "Serie factură duplicată!";
                    return View(factura);
                }

            }

            var programare = await _context.Programari
                .Include(p => p.Masina).ThenInclude(m => m.Client)
                .Include(p => p.Lucrari).ThenInclude(l => l.LucrarePiese).ThenInclude(lp => lp.Piesa)
                .FirstOrDefaultAsync(p => p.Id == factura.ProgramareId);

            ViewBag.Programare = programare;
            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizeazaStatus(int id, StatusPlata statusPlata, MetodaPlata? metodaPlata)
        {
            var factura = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(f => f.Id == id && f.Programare.Masina.Client.UserId == GetUserId());
            if (factura == null) return NotFound();

            factura.StatusPlata = statusPlata;
            factura.MetodaPlata = metodaPlata;
            await _context.SaveChangesAsync();

            TempData["Succes"] = "Statusul facturii a fost actualizat!";
            return RedirectToAction(nameof(Detalii), new { id });
        }

        public async Task<IActionResult> PlataCard(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(f => f.Id == id && f.Programare.Masina.Client.UserId == GetUserId());

            if (factura == null) return NotFound();

            if (factura.StatusPlata == StatusPlata.Platita)
            {
                TempData["Eroare"] = "Factura este deja platita!";
                return RedirectToAction(nameof(Detalii), new { id });
            }

            return View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlataCard(int id, string numarCard, string titular, string expirare, string cvv)
        {
            var factura = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(f => f.Id == id && f.Programare.Masina.Client.UserId == GetUserId());
            if (factura == null) return NotFound();

            await Task.Delay(1500);

            string idTranzactie = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            factura.StatusPlata = StatusPlata.Platita;
            factura.MetodaPlata = MetodaPlata.Card;

            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Plata procesata! ID tranzactie: {idTranzactie}";
            return RedirectToAction(nameof(Detalii), new { id });
        }

        public async Task<IActionResult> Sterge(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(f => f.Id == id && f.Programare.Masina.Client.UserId == GetUserId());

            if (factura == null) return NotFound();
            return View(factura);
        }

        [HttpPost, ActionName("Sterge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmaStergere(int id)
        {
            var factura = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .FirstOrDefaultAsync(f => f.Id == id && f.Programare.Masina.Client.UserId == GetUserId());

            if (factura == null) return NotFound();

            if (factura.StatusPlata == StatusPlata.Platita)
            {
                TempData["Eroare"] = "Factura platita nu poate fi stearsa!";
                return RedirectToAction(nameof(Index));
            }

            _context.Facturi.Remove(factura);
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Factura a fost stearsa!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Print(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturi
                .Include(f => f.Programare).ThenInclude(p => p.Masina).ThenInclude(m => m.Client)
                .Include(f => f.Programare).ThenInclude(p => p.Lucrari).ThenInclude(l => l.LucrarePiese).ThenInclude(lp => lp.Piesa)
                .FirstOrDefaultAsync(f => f.Id == id && f.Programare.Masina.Client.UserId == GetUserId());

            if (factura == null) return NotFound();

            return View(factura);
        }
    }
}